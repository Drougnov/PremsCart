import { useLoad, Metrics } from "./Pages";
import { go } from "./api";
import { useEffect, useState, type FormEvent } from "react";
import { PrivateImage } from "./Media";
import { ProductCard, useWishlist } from "./catalog";
import { Skeleton, EmptyState, ErrorState, toast, confirmAction } from "./UI";
import Icon from "./Icon";

type StoreSummary = {
  logo?: string;
  id: number;
  storeName: string;
  description: string | null;
  ownerId: number;
  ownerName: string;
  productCount: number;
  ownerVerified?: boolean;
  rating?: number | null;
  reviewCount?: number;
};
type StoreProduct = {
  productId: number;
  quantity: number;
  title: string;
  price: number | null;
  transactionType: string;
  status?: string;
  condition?: string;
  location?: string;
  categoryName?: string;
  imageUrl?: string | null;
};
type Store = StoreSummary & { products: StoreProduct[] };
type MyStore = {
  id: number;
  storeName: string;
  description: string | null;
  ownerId: number;
};
type Listing = {
  allowRent?: boolean;
  id: number;
  title: string;
  status: string;
  transactionType: string;
};

async function api<T>(
  url: string,
  token: string,
  method = "GET",
  data?: object,
): Promise<T> {
  const response = await fetch(url, {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(data ? { "Content-Type": "application/json" } : {}),
    },
    body: data ? JSON.stringify(data) : undefined,
  });
  if (response.status === 204) return undefined as T;
  const value = await response.json().catch(() => ({}));
  if (!response.ok)
    throw new Error(
      value.error ?? value.title ?? `Request failed (${response.status})`,
    );
  return value as T;
}

export default function Stores({
  token,
  path,
}: {
  token: string;
  path: string;
}) {
  const summary = useLoad(
    path.startsWith("/dashboard/store") ? "/api/stores/mine/summary" : null,
  );
  const [storeSearch, setStoreSearch] = useState(""),
    [productSearch, setProductSearch] = useState(""),
    [productType, setProductType] = useState(""),
    [lowStock, setLowStock] = useState(false);
  const wish = useWishlist();
  const [role, setRole] = useState("");
  const [stores, setStores] = useState<StoreSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Store | null>(null);
  const [mine, setMine] = useState<MyStore | null>(null);
  const [inventory, setInventory] = useState<StoreProduct[]>([]);
  const [listings, setListings] = useState<Listing[]>([]);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [productId, setProductId] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [stockEdits, setStockEdits] = useState<Record<number, string>>({});
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");

  async function loadMine(authToken = token) {
    const [myStore, items, ownListings] = await Promise.all([
      api<MyStore>("/api/stores/mine", authToken),
      api<StoreProduct[]>("/api/stores/mine/products", authToken),
      api<Listing[]>("/api/products/mine", authToken),
    ]);
    setMine(myStore);
    setName(myStore.storeName);
    setDescription(myStore.description ?? "");
    setInventory(items);
    setListings(ownListings);
  }
  useEffect(() => {
    setRole("");
    setMine(null);
    setSelected(null);
    setInventory([]);
    setStores([]);
    setError("");
    setNotice("");
    if (!token) return;
    let active = true;
    api<{ role: string }>("/api/users/profile", token)
      .then((profile) => {
        if (!active) return;
        setRole(profile.role);
        if(!path.startsWith('/dashboard/store'))void fetch('/api/stores/mine',{headers:{Authorization:`Bearer ${token}`}}).then(async r=>{if(r.status===404)return null;if(!r.ok)throw new Error('Could not check your store.');return r.json()}).then(store=>{if(active)setMine(store)}).catch(e=>{if(active)setError(e.message)});
        if (
          profile.role === "Business Seller" &&
          path.startsWith("/dashboard/store")
        )
          void loadMine().catch((e) => {
            if (active) setError(e.message);
          });
      })
      .catch((e) => {
        if (active) setError(e.message);
      });
    return () => {
      active = false;
    };
  }, [token]);
  useEffect(() => {
    if (!token) return;
    let active = true;
    setLoading(true);
    api<{ items: StoreSummary[]; total: number }>(
      `/api/stores?page=${page}&search=${encodeURIComponent(storeSearch)}`,
      token,
    )
      .then((data) => {
        if (active) {
          setStores(data.items);
          setTotal(data.total);
        }
      })
      .catch((e) => {
        if (active) setError(e.message);
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [token, page, storeSearch]);
  useEffect(() => {
    const open = (event: Event) => {
      const id = (event as CustomEvent<number>).detail;
      if (!id || !token) return;
      api<Store>(`/api/stores/${id}`, token)
        .then(setSelected)
        .catch((e) => setError(e.message));
      document.getElementById("stores")?.scrollIntoView({ behavior: "smooth" });
    };
    window.addEventListener("premscart-open-store", open);
    return () => window.removeEventListener("premscart-open-store", open);
  }, [token]);
  async function action(callback: () => Promise<unknown>, message: string) {
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await callback();
      await loadMine();
      setNotice(message);
      toast(message);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Request failed.");
    } finally {
      setBusy(false);
    }
  }
  async function saveProfile(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError("");
    setNotice("");
    try {
      if (!mine) {
        const result = await api<{ id: number; token: string }>(
          "/api/stores",
          token,
          "POST",
          { storeName: name, description },
        );
        sessionStorage.setItem("premscart-token", result.token);
        window.dispatchEvent(new Event("premscart-auth"));
        setRole("Business Seller");
      } else
        await api("/api/stores/mine", token, "PUT", {
          storeName: name,
          description,
        });
      setNotice(
        mine
          ? "Store profile saved."
          : "Store created. Your business account is ready.",
      );
      await loadMine(sessionStorage.getItem("premscart-token") || token);
      const data = await api<{ items: StoreSummary[]; total: number }>(
        `/api/stores?page=${page}&search=${encodeURIComponent(storeSearch)}`,
        sessionStorage.getItem("premscart-token") || token,
      );
      setStores(data.items);
      setTotal(data.total);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not save store.");
    } finally {
      setBusy(false);
    }
  }
  useEffect(() => {
    const id = Number(path.split("/")[2]);
    if (path.startsWith("/stores/") && id && token) void openStore(id);
  }, [path, token]);
  async function openStore(id: number) {
    if (path !== `/stores/${id}`) {
      go(`/stores/${id}`);
      return;
    }
    setError("");
    try {
      setSelected(await api<Store>(`/api/stores/${id}`, token));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Store unavailable.");
    }
  }
  return (
    <section id="stores" className="feature-section stores-section">
      <div className="section-heading">
        <div>
          <span className="eyebrow">STUDENT BUSINESSES</span>
          <h2>Campus stores</h2>
          <p>Discover the small businesses built by your classmates.</p>
        </div>
      </div>
      {!token ? (
        <p className="feature-empty">Sign in to browse stores or create one.</p>
      ) : (
        <>
          {error && (
            <ErrorState
              message={error}
              retry={() => {
                if (selected) void openStore(selected.id);
                else window.location.reload();
              }}
            />
          )}
          {notice && (
            <p className="form-success" role="status">
              {notice}
            </p>
          )}
          {selected && (
            <div className="storefront">
              <a className="inline-link" href="/stores">
                ← All student stores
              </a>
              <header className="storefront-banner">
                <div className="storefront-logo">
                  <PrivateImage
                    url={selected.logo}
                    token={token}
                    alt={selected.storeName}
                  />
                </div>
                <div>
                  <span className="eyebrow">A STUDENT BUSINESS</span>
                  <h1>{selected.storeName}</h1>
                  <p>
                    {selected.description ||
                      "Useful things from your campus community."}
                  </p>
                  <a
                    href={`/students/${selected.ownerId}`}
                    className="store-owner"
                  >
                    <Icon name="shield" /> {selected.ownerName} · University
                    email verified
                  </a>
                  <span className="store-rating">
                    <Icon name="star" />
                    {selected.reviewCount
                      ? `${Number(selected.rating).toFixed(1)} · ${selected.reviewCount} reviews`
                      : "No reviews yet"}
                  </span>
                </div>
              </header>
              <div className="storefront-toolbar">
                <label className="search-field">
                  <span className="sr-only">Search this store</span>
                  <input
                    placeholder="Search this store…"
                    value={productSearch}
                    onChange={(e) => setProductSearch(e.target.value)}
                  />
                </label>
                <label>
                  <span className="sr-only">Store listing type</span>
                  <select
                    aria-label="Store listing type"
                    value={productType}
                    onChange={(e) => setProductType(e.target.value)}
                  >
                    <option value="">All products</option>
                    <option value="Sell">For sale</option>
                    <option value="Giveaway">Giveaways</option>
                  </select>
                </label>
                <span>{selected.products.length} available listings</span>
              </div>
              {selected.products.filter(
                (p) =>
                  (!productType || p.transactionType === productType) &&
                  p.title.toLowerCase().includes(productSearch.toLowerCase()),
              ).length ? (
                <div className="catalog-grid">
                  {selected.products
                    .filter(
                      (p) =>
                        (!productType || p.transactionType === productType) &&
                        p.title
                          .toLowerCase()
                          .includes(productSearch.toLowerCase()),
                    )
                    .map((item) => (
                      <ProductCard
                        key={item.productId}
                        product={{
                          ...item,
                          id: item.productId,
                          status: "Available",
                          sellerName: selected.ownerName,
                          sellerVerified: true,
                        }}
                        token={token}
                        saved={wish.saved.includes(item.productId)}
                        onSave={() => wish.toggle(item.productId)}
                      />
                    ))}
                </div>
              ) : (
                <EmptyState
                  title="No products here just yet."
                  message="Try another search or explore other student stores."
                  href="/stores"
                  label="Explore stores"
                  icon="store"
                />
              )}
            </div>
          )}
          {path === "/stores" && (
            <>
              <div className="storefront-toolbar">
                <label className="search-field">
                  Find a student store
                  <input
                    placeholder="Search stores by name"
                    value={storeSearch}
                    onChange={(e) => {
                      setStoreSearch(e.target.value);
                      setPage(1);
                    }}
                  />
                </label>
                <a className="button button-primary" href="/dashboard/store">
                  {mine ? "Manage my store" : "Open your own store"} <Icon name="arrow" />
                </a>
              </div>
              {loading ? (
                <Skeleton count={3} label="Loading campus stores" />
              ) : stores.length ? (
                <div className="store-grid">
                  {stores.map((s) => (
                    <article className="store-directory-card" key={s.id}>
                      <div className="store-directory-logo">
                        <PrivateImage
                          url={s.logo}
                          token={token}
                          alt={s.storeName}
                        />
                      </div>
                      <span className="verified-label">
                        <Icon name="shield" />
                        University email verified
                      </span>
                      <h2>{s.storeName}</h2>
                      <p>
                        {s.description || "Good things from your classmates."}
                      </p>
                      <small>
                        {s.ownerName} · {s.productCount} available listings
                      </small>
                      <a
                        className="button button-secondary"
                        href={`/stores/${s.id}`}
                      >
                        Visit store <Icon name="arrow" />
                      </a>
                    </article>
                  ))}
                </div>
              ) : (
                <EmptyState
                  title="A little room for a new student business."
                  message="No stores match your search. Explore listings or create your own campus shop."
                  href="/marketplace"
                  label="Explore marketplace"
                  icon="store"
                />
              )}
            </>
          )}
          {path === "/stores" && total > 12 && (
            <div className="pagination">
              <button
                disabled={page === 1}
                onClick={() => setPage((v) => v - 1)}
              >
                Previous
              </button>
              <span>
                Page {page} of {Math.ceil(total / 12)}
              </span>
              <button
                disabled={page * 12 >= total}
                onClick={() => setPage((v) => v + 1)}
              >
                Next
              </button>
            </div>
          )}
          {path.startsWith("/dashboard/store") &&
            (role === "Student" || role === "Business Seller") && (
              <div className="community-form">
                <h3>
                  {mine ? "Manage my store" : "Create your student store"}
                </h3>
                {(path !== "/dashboard/store/inventory" || !mine) && (
                  <form className="store-form" onSubmit={saveProfile}>
                    <label>
                      Store name
                      <input
                        required
                        maxLength={100}
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                      />
                    </label>
                    <label>
                      Description
                      <textarea
                        maxLength={1000}
                        rows={3}
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                      />
                    </label>
                    <button disabled={busy} className="market-action">
                      {busy
                        ? "Saving…"
                        : mine
                          ? "Save store profile"
                          : "Create store"}
                    </button>
                  </form>
                )}
                {mine && (
                  <>
                    {summary.data && <Metrics data={summary.data} />}
                    <p>
                      <a href={`/stores/${mine.id}`}>Visit my store →</a>
                    </p>
                    <p>
                      <a href="/dashboard/store/inventory">
                        Manage inventory →
                      </a>
                    </p>
                    {path === "/dashboard/store/inventory" && (
                      <>
                        <h3>Inventory</h3>
                        <label className="check-field">
                          <input
                            type="checkbox"
                            checked={lowStock}
                            onChange={(e) => setLowStock(e.target.checked)}
                          />
                          Show low stock only (fewer than 3)
                        </label>
                        <p>
                          {inventory.filter((i) => i.quantity < 3).length}{" "}
                          low-stock listings
                        </p>
                        <p>
                          Create a marketplace listing first, then add it to
                          your store.
                        </p>
                        <div className="transaction-buttons">
                          <label>
                            My listing
                            <select
                              value={productId}
                              onChange={(e) => setProductId(e.target.value)}
                            >
                              <option value="">Select available listing</option>
                              {listings
                                .filter(
                                  (p) =>
                                    p.status === "Available" &&
                                    p.transactionType !== "Rent" && !p.allowRent &&
                                    !inventory.some(
                                      (i) => i.productId === p.id,
                                    ),
                                )
                                .map((p) => (
                                  <option value={p.id} key={p.id}>
                                    {p.title}
                                  </option>
                                ))}
                            </select>
                          </label>
                          <label>
                            Quantity
                            <input
                              type="number"
                              min="1"
                              max="10000"
                              value={quantity}
                              onChange={(e) => setQuantity(e.target.value)}
                            />
                          </label>
                          <button
                            disabled={busy || !productId}
                            onClick={() => {
                              void action(async () => {
                                await api(
                                  "/api/stores/mine/products",
                                  token,
                                  "POST",
                                  {
                                    productId: Number(productId),
                                    quantity: Number(quantity),
                                  },
                                );
                                setProductId("");
                              }, "Product added to store.");
                            }}
                          >
                            Add to store
                          </button>
                        </div>
                        {inventory
                          .filter((item) => !lowStock || item.quantity < 3)
                          .map((item) => (
                            <div
                              className="store-inventory-row"
                              key={item.productId}
                            >
                              <div>
                                <strong>{item.title}</strong>
                                <p>
                                  {item.status} · {item.quantity} available
                                </p>
                              </div>
                              <div className="transaction-buttons">
                                <label>
                                  In stock
                                  <input
                                    type="number"
                                    min="0"
                                    max="10000"
                                    value={
                                      stockEdits[item.productId] ??
                                      String(item.quantity)
                                    }
                                    onChange={(e) =>
                                      setStockEdits((v) => ({
                                        ...v,
                                        [item.productId]: e.target.value,
                                      }))
                                    }
                                  />
                                </label>
                                <button
                                  disabled={busy}
                                  onClick={() => {
                                    void action(async () => {
                                      await api(
                                        `/api/stores/mine/products/${item.productId}`,
                                        token,
                                        "PUT",
                                        {
                                          quantity: Number(
                                            stockEdits[item.productId] ??
                                              item.quantity,
                                          ),
                                        },
                                      );
                                      setStockEdits((v) => {
                                        const next = { ...v };
                                        delete next[item.productId];
                                        return next;
                                      });
                                    }, "Stock updated.");
                                  }}
                                >
                                  Save stock
                                </button>
                                <button
                                  disabled={busy}
                                  onClick={async () => {
                                    if (
                                      await confirmAction(
                                        "Remove this item from your store?",
                                        item.title,
                                        "Remove from store",
                                        true,
                                      )
                                    )
                                      void action(
                                        () =>
                                          api(
                                            `/api/stores/mine/products/${item.productId}`,
                                            token,
                                            "DELETE",
                                          ),
                                        "Listing removed from store.",
                                      );
                                  }}
                                >
                                  Remove
                                </button>
                              </div>
                            </div>
                          ))}
                      </>
                    )}
                  </>
                )}
              </div>
            )}
        </>
      )}
    </section>
  );
}
