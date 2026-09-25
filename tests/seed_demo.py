"""Seed a running local PremsCart instance with predictable demo data.

Requires only Python 3.10+ standard library. It uses the public API and Mailpit,
so passwords are hashed by the application exactly like normal accounts.

Run after `docker compose up --build`:
    python tests/seed_demo.py
"""
import json, os, re, time, uuid, urllib.request, urllib.error
from datetime import datetime, timedelta, timezone
from pathlib import Path

BASE = os.getenv("API_URL", "http://localhost:5000")
MAIL = os.getenv("MAILPIT_URL", "http://localhost:8025")
PASSWORD = os.getenv("DEMO_PASSWORD", "CampusDemo!123")
HERE = Path(__file__).resolve().parent

class ApiError(RuntimeError):
    def __init__(self, path, code, raw):
        super().__init__(f"{path}: HTTP {code}: {raw[:400]!r}")
        self.path, self.code, self.raw = path, code, raw

def call(path, method="GET", body=None, token=None, allowed=()):
    headers = {"Accept": "application/json"}
    data = None
    if body is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body).encode()
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=25) as r:
            code, raw = r.status, r.read()
    except urllib.error.HTTPError as e:
        code, raw = e.code, e.read()
    if not (200 <= code < 300) and code not in allowed:
        raise ApiError(path, code, raw)
    if not raw:
        return code, {}
    try:
        return code, json.loads(raw)
    except json.JSONDecodeError:
        return code, raw.decode(errors="replace")

def code_for(email, purpose="verification"):
    for _ in range(45):
        with urllib.request.urlopen(MAIL + "/api/v1/messages", timeout=10) as r:
            messages = json.load(r).get("messages", [])
        for m in messages:
            if purpose in m.get("Subject", "").lower() and any(a.get("Address", "").lower() == email for a in m.get("To", [])):
                with urllib.request.urlopen(MAIL + "/api/v1/message/" + m["ID"], timeout=10) as r:
                    detail = json.load(r)
                found = re.search(r"\b\d{6}\b", detail.get("Text", ""))
                if found:
                    return found.group()
        time.sleep(.3)
    raise RuntimeError("Verification email not found in Mailpit for " + email)

def ensure_account(first, last, email):
    code, login = call("/api/auth/login", "POST", {"email": email, "password": PASSWORD}, allowed=(401,403))
    if code == 200:
        return login["token"], login["profile"]["id"], login["profile"]["role"]

    reg_code, _ = call("/api/auth/register", "POST", {
        "firstName": first, "lastName": last, "email": email, "password": PASSWORD
    }, allowed=(409,))
    if reg_code == 409:
        # Existing but unverified account: ask for a fresh code. 429 is fine if one was just sent.
        call("/api/auth/resend-code", "POST", {"email": email}, allowed=(429,))
    otp = code_for(email, "verification")
    verify_code, _ = call("/api/auth/verify-email", "POST", {"email": email, "code": otp}, allowed=(400,))
    # A 400 can simply mean an earlier request already verified it; login is the source of truth.
    _, login = call("/api/auth/login", "POST", {"email": email, "password": PASSWORD})
    return login["token"], login["profile"]["id"], login["profile"]["role"]

def upload_image(product_id, token, filename):
    path = HERE / "demo-images" / filename
    boundary = "----PremsCart" + uuid.uuid4().hex
    file_bytes = path.read_bytes()
    body = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="file"; filename="{path.name}"\r\n'
        "Content-Type: image/png\r\n\r\n"
    ).encode() + file_bytes + f"\r\n--{boundary}--\r\n".encode()
    req = urllib.request.Request(
        BASE + f"/api/products/{product_id}/images", data=body, method="POST",
        headers={"Authorization": "Bearer " + token, "Content-Type": f"multipart/form-data; boundary={boundary}"}
    )
    try:
        with urllib.request.urlopen(req, timeout=30) as r:
            return r.status
    except urllib.error.HTTPError as e:
        if e.code != 400:
            raise
        return e.code

def ensure_product(token, title, description, category_id, price, kind, condition, location, negotiable, image_file):
    _, mine = call("/api/products/mine", token=token)
    existing = next((x for x in mine if x["title"] == title), None)
    if existing:
        product_id = existing["id"]
    else:
        _, created = call("/api/products", "POST", {
            "title": title, "description": description, "categoryId": category_id,
            "price": price, "transactionType": kind, "condition": condition,
            "status": "Available", "location": location, "isNegotiable": negotiable
        }, token)
        product_id = created["id"]
    _, detail = call(f"/api/products/{product_id}", token=token)
    if not detail.get("images"):
        upload_image(product_id, token, image_file)
    return product_id

def ensure_wanted(token, title, description, budget, category_id):
    _, mine = call("/api/wanted/mine", token=token)
    if any(x["title"] == title for x in mine):
        return
    call("/api/wanted", "POST", {"title": title, "description": description, "budget": budget, "categoryId": category_id, "status": "Open"}, token)

def main():
    print("Connecting to", BASE)
    _, health = call("/api/health")

    buyer_token, buyer_id, _ = ensure_account("Ayesha", "Rahman", "demo.buyer_44001@bscse.puc.ac.bd")
    seller_token, seller_id, _ = ensure_account("Nafis", "Ahmed", "demo.seller_44002@bscse.puc.ac.bd")
    shop_token, shop_id, shop_role = ensure_account("Samira", "Khan", "demo.shop_44003@bscse.puc.ac.bd")

    _, categories = call("/api/products/categories", token=buyer_token)
    cat = {x["categoryName"]: x["id"] for x in categories}
    _, locations = call("/api/transactions/locations", token=buyer_token)
    loc = {x["locationName"]: x["id"] for x in locations}

    # Create shop once. Creating a store upgrades the account to Business Seller and returns a fresh token.
    store_code, store = call("/api/stores/mine", token=shop_token, allowed=(404,))
    if store_code == 404:
        _, created = call("/api/stores", "POST", {
            "storeName": "PU Tech Corner",
            "description": "Affordable study essentials and electronics from a verified Premier University student seller."
        }, shop_token)
        shop_token = created["token"]
        _, store = call("/api/stores/mine", token=shop_token)

    products = {}
    products["book"] = ensure_product(seller_token, "Data Structures & Algorithms Book", "Clean copy with highlighted key topics. Great for CSE coursework and exam preparation.", cat["Books"], 450, "Sell", "Good", "Library", True, "dsa-book.png")
    products["calculator"] = ensure_product(seller_token, "Scientific Calculator", "Fully working scientific calculator with a fresh battery and protective cover.", cat["Electronics"], 950, "Sell", "Like new", "Main gate", False, "calculator.png")
    products["rental"] = ensure_product(seller_token, "Scientific Calculator — Daily Rental", "Rent a scientific calculator for exams or a short course. Return it in the same condition after your selected duration.", cat["Electronics"], 40, "Rent", "Good", "Library", False, "calculator.png")
    products["notes"] = ensure_product(seller_token, "CSE Semester Notes Bundle", "Printed notes for core CSE subjects. Giving these away to a student who can use them.", cat["Academic Materials"], 0, "Giveaway", "Good", "Library", False, "notes.png")
    products["mouse"] = ensure_product(seller_token, "Wireless Mouse - Demo Sale", "Comfortable wireless mouse used for one semester. This listing is used to populate transaction history.", cat["Electronics"], 650, "Sell", "Good", "Main gate", False, "mouse.png")

    products["hoodie"] = ensure_product(shop_token, "Premier CSE Hoodie", "Comfortable university-style hoodie for everyday campus wear.", cat["Clothing"], 850, "Sell", "New", "Main gate", False, "hoodie.png")
    products["arduino"] = ensure_product(shop_token, "Arduino Uno Starter Kit", "Arduino Uno, breadboard, jumper wires, LEDs and basic sensors for student projects.", cat["Electronics"], 1450, "Sell", "New", "Main gate", True, "arduino.png")
    products["notebooks"] = ensure_product(shop_token, "Notebook Bundle - 5 Pack", "Five ruled notebooks suitable for class notes, labs and assignments.", cat["Academic Materials"], 220, "Sell", "New", "Canteen", False, "notebooks.png")

    _, stock = call("/api/stores/mine/products", token=shop_token)
    stocked = {x["productId"] for x in stock}
    for key, qty in (("hoodie", 8), ("arduino", 5), ("notebooks", 15)):
        if products[key] not in stocked:
            call("/api/stores/mine/products", "POST", {"productId": products[key], "quantity": qty}, shop_token)

    ensure_wanted(buyer_token, "Looking for Discrete Mathematics textbook", "Need a reasonably priced copy in readable condition. Any recent edition is fine.", 500, cat["Books"])
    ensure_wanted(buyer_token, "Need a USB-C hub", "Looking for a working USB-C hub with HDMI and at least two USB ports.", 900, cat["Electronics"])

    # Wishlist and an open conversation make the student dashboard feel populated.
    call(f"/api/wishlist/{products['arduino']}", "POST", {}, buyer_token)
    call("/api/chat/conversations", "POST", {"productId": products["book"]}, buyer_token, allowed=(409,))

    # Leave one active negotiation.
    offer_code, _ = call("/api/transactions/offers", "POST", {"productId": products["book"], "amount": 400}, buyer_token, allowed=(409,))

    # Leave one pending giveaway request.
    call("/api/transactions/orders", "POST", {"productId": products["notes"]}, buyer_token, allowed=(409,))

    # Create one completed sale + review, but only if it has not already been completed.
    _, buyer_orders = call("/api/transactions/orders", token=buyer_token)
    mouse_orders = [o for o in buyer_orders if o["productId"] == products["mouse"]]
    if not any(o["status"] == "Completed" for o in mouse_orders):
        active = next((o for o in mouse_orders if o["status"] in ("Pending", "Accepted", "Pickup scheduled")), None)
        if active is None:
            code, created = call("/api/transactions/orders", "POST", {"productId": products["mouse"]}, buyer_token, allowed=(409,))
            if code == 201:
                active = {"id": created["id"], "status": "Pending"}
        if active:
            oid = active["id"]
            if active["status"] == "Pending":
                call(f"/api/transactions/orders/{oid}/accept", "POST", {}, seller_token)
            _, refreshed = call("/api/transactions/orders", token=buyer_token)
            order = next(x for x in refreshed if x["id"] == oid)
            if order["status"] == "Accepted":
                pickup = (datetime.now(timezone.utc) + timedelta(seconds=4)).isoformat().replace("+00:00", "Z")
                call(f"/api/transactions/orders/{oid}/pickup", "POST", {"locationId": loc["Main gate"], "pickupTime": pickup}, buyer_token)
                call(f"/api/transactions/orders/{oid}/pickup/confirm", "POST", {}, seller_token)
                time.sleep(4.2)
                call(f"/api/transactions/orders/{oid}/complete", "POST", {}, buyer_token)
                call("/api/community/reviews", "POST", {"orderId": oid, "rating": 5, "comment": "Friendly seller and smooth campus handoff."}, buyer_token, allowed=(409,))

    print("\nDemo data is ready.")
    print("Buyer:  demo.buyer_44001@bscse.puc.ac.bd /", PASSWORD)
    print("Seller: demo.seller_44002@bscse.puc.ac.bd /", PASSWORD)
    print("Shop:   demo.shop_44003@bscse.puc.ac.bd /", PASSWORD)
    print("Store:  PU Tech Corner")
    print("Open:   http://localhost:5173")
    print("Mailpit: http://localhost:8025")

if __name__ == "__main__":
    main()
