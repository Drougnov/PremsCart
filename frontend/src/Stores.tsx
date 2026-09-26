import { useLoad, Metrics, PhotoUpload } from "./Pages";
import { go } from "./api";
import { useEffect, useState, type FormEvent } from "react";
import { PrivateImage } from "./Media";
import { ProductCard, useWishlist } from "./catalog";
import { Skeleton, EmptyState, ErrorState, toast } from "./UI";
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
  isVisible: boolean;
  sortOrder: number;
  title: string;
  price: number | null;
  allowRent?: boolean;
  rentalPrice?: number | null;
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
  logo?: string | null;
  ownerId: number;
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
    [productType, setProductType] = useState("");
  const wish = useWishlist();
  const [role, setRole] = useState("");
  const [stores, setStores] = useState<StoreSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Store | null>(null);
  const [mine, setMine] = useState<MyStore | null>(null);
  const [inventory, setInventory] = useState<StoreProduct[]>([]);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [stockEdits, setStockEdits] = useState<Record<number, string>>({});
  const [draggingId, setDraggingId] = useState<number | null>(null);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");

  async function loadMine(authToken = token) {
    const [myStore, items] = await Promise.all([
      api<MyStore>("/api/stores/mine", authToken),
      api<StoreProduct[]>("/api/stores/mine/products", authToken),
    ]);
    setMine(myStore);
    setName(myStore.storeName);
    setDescription(myStore.description ?? "");
    setInventory(items);
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
  function saveInventoryOrder(next: StoreProduct[]) {
    if (busy) return;
    setInventory(next);
    setDraggingId(null);
    void action(
      () => api('/api/stores/mine/products/order', token, 'PUT', { productIds: next.map(x => x.productId) }),
      'Store item order saved.'
    );
  }
  function reorderInventory(targetId: number) {
    if (draggingId == null || draggingId === targetId || busy) return;
    const from = inventory.findIndex(x => x.productId === draggingId);
    const to = inventory.findIndex(x => x.productId === targetId);
    if (from < 0 || to < 0) return;
    const next = [...inventory];
    const [moved] = next.splice(from,1);
    next.splice(to,0,moved);
    saveInventoryOrder(next);
  }
  function moveInventory(productId: number, direction: -1 | 1) {
    if (busy) return;
    const from = inventory.findIndex(x => x.productId === productId);
    const to = from + direction;
    if (from < 0 || to < 0 || to >= inventory.length) return;
    const next = [...inventory];
    [next[from], next[to]] = [next[to], next[from]];
    saveInventoryOrder(next);
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
      {path === "/stores" && <div className="section-heading">
        <div>
          <span className="eyebrow">STUDENT BUSINESSES</span>
          <h1>Campus stores</h1>
          <p>Discover the small businesses built by your classmates.</p>
        </div>
      </div>}
      {path.startsWith("/dashboard/store") && <div className="page-heading">
        <div>
          <span className="eyebrow">YOUR STOREFRONT</span>
          <h1>{mine ? "My store" : "Create your store"}</h1>
          <p>{path === "/dashboard/store/inventory" ? "Choose which posted items appear in your store and arrange their order." : "Update the information students see on your public store page."}</p>
        </div>
        {mine&&<a className="button button-secondary compact" href={`/stores/${mine.id}`}>View public store <Icon name="arrow"/></a>}
      </div>}
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
                  <span className="sr-only">Store item type</span>
                  <select
                    aria-label="Store item type"
                    value={productType}
                    onChange={(e) => setProductType(e.target.value)}
                  >
                    <option value="">All products</option>
                    <option value="Sell">For sale</option>
                    <option value="Rent">For rent</option>
                    <option value="Giveaway">Giveaway</option>
                  </select>
                </label>
                <span>{selected.products.length} item{selected.products.length===1?"":"s"} in this store</span>
              </div>
              {selected.products.filter(
                (p) =>
                  (!productType || p.transactionType === productType || (productType === "Rent" && p.allowRent)) &&
                  p.title.toLowerCase().includes(productSearch.toLowerCase()),
              ).length ? (
                <div className="catalog-grid">
                  {selected.products
                    .filter(
                      (p) =>
                        (!productType || p.transactionType === productType || (productType === "Rent" && p.allowRent)) &&
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
                  title="No items here yet."
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
                        {s.ownerName} · {s.productCount} item{s.productCount===1?"":"s"}
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
                  message="No stores match your search. Browse campus items or create your own store."
                  href="/marketplace"
                  label="Browse items"
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
                <div className="panel-heading store-management-heading"><div><h2>{path === "/dashboard/store/inventory" && mine ? "Store items" : mine ? "Store information" : "Store information"}</h2><p>{path === "/dashboard/store/inventory" && mine ? "Show or hide items and control the order shoppers see." : mine ? "Keep your logo, store name, and description together in one place." : "Start with a name and short description. Your posted items will appear automatically."}</p></div></div>
                {(path !== "/dashboard/store/inventory" || !mine) && (
                  <section className="store-information-panel">
                    {mine&&<div className="store-logo-editor"><div className="store-logo-preview"><PrivateImage url={mine.logo} token={token} alt={`${mine.storeName} logo`}/></div><PhotoUpload store onSaved={()=>void loadMine()}/></div>}
                    <form className="store-form" onSubmit={saveProfile}>
                      <label>Store name<input required maxLength={100} value={name} onChange={(e) => setName(e.target.value)}/></label>
                      <label>Description<textarea maxLength={1000} rows={4} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Tell students what your store offers."/></label>
                      <button disabled={busy} className="button button-primary">{busy ? "Saving…" : mine ? "Save store information" : "Create store"}</button>
                    </form>
                  </section>
                )}
                {mine && (
                  <>
                    {path === "/dashboard/store" && <>
                      {summary.data && <Metrics data={summary.data} />}
                      
                    </>}
                    {path === "/dashboard/store/inventory" && (
                      <section className="store-merchandising">
                        <div className="panel-heading"><div><span className="eyebrow">YOUR STOREFRONT</span><h3>Arrange store items</h3><p>Every item you post is added here automatically. Drag items to change their order, or hide an item from your store without deleting the post.</p></div></div>
                        {!inventory.length ? <EmptyState title="Your store has no items yet." message="Post an item and it will be added to your store automatically." href="/listings/new" label="Post an item" icon="store"/> : <div className="store-merch-list" aria-label="Store items in display order">
                          {inventory.map((item,index) => {
                            const singleItem = item.transactionType === 'Rent' || !!item.allowRent;
                            return <article
                              className={`store-merch-row ${item.isVisible?'visible':'hidden'} ${draggingId===item.productId?'dragging':''}`}
                              key={item.productId}
                              draggable={!busy}
                              onDragStart={() => setDraggingId(item.productId)}
                              onDragEnd={() => setDraggingId(null)}
                              onDragOver={e => e.preventDefault()}
                              onDrop={e => { e.preventDefault(); reorderInventory(item.productId) }}
                            >
                              <button type="button" className="drag-handle" disabled={busy} aria-label={`Move ${item.title}. Drag to reorder`} title="Drag to reorder"><Icon name="menu"/><span>{index+1}</span></button>
                              <div className="store-merch-photo"><PrivateImage url={item.imageUrl} token={token} alt=""/></div>
                              <div className="store-merch-copy"><div className="store-merch-title"><strong>{item.title}</strong><span className={`store-display-state ${item.isVisible?'shown':'hidden'}`}>{item.isVisible?'Shown in store':'Hidden from store'}</span></div><p>{item.transactionType==='Giveaway'?'Giveaway':item.transactionType==='Rent'?`Rent · Tk ${item.price ?? 0}/day`:item.allowRent?`For sale · also rentable`: 'For sale'} · {item.status}</p><small>{item.categoryName}{item.condition?` · ${item.condition}`:''}</small></div>
                              <div className="store-merch-actions">
                                <div className="store-order-buttons" aria-label={`Change ${item.title} position`}>
                                  <button type="button" disabled={busy || index === 0} onClick={() => moveInventory(item.productId, -1)} aria-label={`Move ${item.title} up`}><Icon name="arrow"/>Up</button>
                                  <button type="button" disabled={busy || index === inventory.length - 1} onClick={() => moveInventory(item.productId, 1)} aria-label={`Move ${item.title} down`}><Icon name="arrow"/>Down</button>
                                </div>
                                <button type="button" className={`visibility-button ${item.isVisible?'on':''}`} disabled={busy} aria-pressed={item.isVisible} onClick={() => void action(() => api(`/api/stores/mine/products/${item.productId}/display`, token, 'PUT', { isVisible: !item.isVisible }), item.isVisible?'Item hidden from your store.':'Item shown in your store.')}><Icon name={item.isVisible?'check':'close'}/>{item.isVisible?'Hide from store':'Show in store'}</button>
                                {!singleItem ? <div className="stock-editor"><label>Stock<input type="number" min="0" max="10000" step="1" inputMode="numeric" value={stockEdits[item.productId] ?? String(item.quantity)} onChange={e => setStockEdits(v => ({...v,[item.productId]:e.target.value}))}/></label><button type="button" disabled={busy || (stockEdits[item.productId]??String(item.quantity))===String(item.quantity)} onClick={() => void action(async () => { await api(`/api/stores/mine/products/${item.productId}`,token,'PUT',{quantity:Number(stockEdits[item.productId]??item.quantity)}); setStockEdits(v=>{const n={...v};delete n[item.productId];return n}) },'Stock updated.')}>Save stock</button></div> : <span className="single-item-note"><Icon name="package"/>Single item · availability follows the post</span>}
                                <a className="button button-secondary compact" href={`/listings/${item.productId}/edit`}>Edit item</a>
                              </div>
                            </article>
                          })}
                        </div>}
                        <p className="helper-copy store-drag-help"><Icon name="menu"/> Tip: drag the handle, or use the Up and Down buttons on a phone or keyboard. Changes are saved automatically.</p>
                      </section>
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
