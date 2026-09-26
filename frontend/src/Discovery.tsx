import { useEffect, useState } from "react";
import { api, go } from "./api";
import { ProductCard, useWishlist, type Product } from "./catalog";
import { EmptyState, ErrorState, Skeleton } from "./UI";
import Icon from "./Icon";
export function GlobalSearch({ userId }: { userId?: number }) {
  const [value, setValue] = useState(""),
    [recent, setRecent] = useState<string[]>([]),
    [suggestions, setSuggestions] = useState<string[]>([]);
  const key = `premscart-searches-${userId ?? "guest"}`;
  useEffect(() => {
    try {
      const data = JSON.parse(localStorage.getItem(key) ?? "[]");
      setRecent(
        Array.isArray(data)
          ? data.filter((x) => typeof x === "string").slice(0, 5)
          : [],
      );
    } catch {}
  }, [key]);
  useEffect(() => {
    let active = true;
    if (!userId || value.trim().length < 2) {
      setSuggestions([]);
      return;
    }
    const timer = setTimeout(
      () =>
        api(`/api/products?type=Sell&search=${encodeURIComponent(value.trim())}`)
          .then((r) => {
            if (active)
              setSuggestions(r.items.slice(0, 5).map((p: Product) => p.title));
          })
          .catch(() => {}),
      300,
    );
    return () => {
      active = false;
      clearTimeout(timer);
    };
  }, [value, userId]);
  return (
    <form
      className="global-search"
      role="search"
      onSubmit={(e) => {
        e.preventDefault();
        if (!value.trim()) return;
        const next = [
          value.trim(),
          ...recent.filter((x) => x !== value.trim()),
        ].slice(0, 5);
        try {
          localStorage.setItem(key, JSON.stringify(next));
        } catch {}
        setRecent(next);
        go(`/marketplace?search=${encodeURIComponent(value.trim())}`);
      }}
    >
      <Icon name="search" />
      <label className="sr-only" htmlFor="global-discovery">
        Search items for sale
      </label>
      <input
        id="global-discovery"
        maxLength={100}
        list="search-suggestions"
        placeholder="Search items for sale"
        value={value}
        onChange={(e) => setValue(e.target.value)}
      />
      <datalist id="search-suggestions">
        {Array.from(new Set([...suggestions, ...recent])).map((x) => (
          <option key={x} value={x} />
        ))}
      </datalist>
      <button type="submit" aria-label="Search items for sale">
        <Icon name="arrow" />
      </button>
    </form>
  );
}
export function HomeDiscovery({ token }: { token: string }) {
  const [categories, setCategories] = useState<
      { id: number; categoryName: string }[]
    >([]),
    [stats, setStats] = useState<any>(null);
  useEffect(() => {
    let active = true;
    api("/api/products/categories")
      .then((d) => {
        if (active) setCategories(d);
      })
      .catch(() => {});
    api("/api/discovery/stats")
      .then((d) => {
        if (active) setStats(d);
      })
      .catch(() => {});
    return () => {
      active = false;
    };
  }, []);
  return (
    <section className="home-section live-discovery">
      <div className="home-section-heading">
        <div>
          <span className="eyebrow">HAPPENING AROUND YOU</span>
          <h2>A campus full of possibilities.</h2>
        </div>
        <a className="inline-link" href="/marketplace">
          Browse campus items
          <Icon name="arrow" />
        </a>
      </div>
      {stats && (
        <div className="community-counts">
          <span>
            <strong>{stats.activeListings.toLocaleString()}</strong> available
            items
          </span>
          <span>
            <strong>{stats.verifiedMembers.toLocaleString()}</strong>{" "}
            email-verified members
          </span>
          <span>
            <strong>{stats.completedExchanges.toLocaleString()}</strong>{" "}
            completed exchanges
          </span>
        </div>
      )}
      {categories.length > 0 && (
        <nav className="category-shortcuts" aria-label="Marketplace categories">
          {categories.map((c) => (
            <a key={c.id} href={`/marketplace?categoryId=${c.id}`}>
              <Icon
                name={
                  c.categoryName === "Books"
                    ? "listing"
                    : c.categoryName === "Electronics"
                      ? "inventory"
                      : c.categoryName === "Clothing"
                        ? "market"
                        : "package"
                }
              />
              {c.categoryName}
              <Icon name="arrow" />
            </a>
          ))}
        </nav>
      )}
      {token ? (
        <ListingShelf token={token} />
      ) : (
        <div className="members-preview">
          <span>
            <Icon name="lock" />
          </span>
          <div>
            <h3>Real finds, shared within your campus.</h3>
            <p>Sign in to see current campus items, photos, and student sellers.</p>
          </div>
          <a
            className="button button-primary"
            href="/login?next=%2Fmarketplace"
          >
            See items for sale
            <Icon name="arrow" />
          </a>
        </div>
      )}
    </section>
  );
}
export function ListingShelf({
  token,
  type,
}: {
  token: string;
  type?: string;
}) {
  const [items, setItems] = useState<Product[]>([]),
    [loading, setLoading] = useState(true),
    [error, setError] = useState(""),
    [version, setVersion] = useState(0);
  const wish = useWishlist();
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError("");
    api(`/api/products?sort=newest${type ? `&type=${type}` : ""}`)
      .then((d) => {
        if (active) setItems(d.items.slice(0, 4));
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
  }, [type, version]);
  return error ? (
    <ErrorState message={error} retry={() => setVersion((v) => v + 1)} />
  ) : loading ? (
    <Skeleton />
  ) : items.length ? (
    <div className="catalog-grid">
      {items.map((p) => (
        <ProductCard
          key={p.id}
          product={p}
          token={token}
          saved={wish.saved.includes(p.id)}
          onSave={() => wish.toggle(p.id)}
          disabled={wish.pending.includes(p.id)}
        />
      ))}
    </div>
  ) : (
    <EmptyState
      title={
        type === "Rent"
          ? "Have something useful to lend?"
          : "The next good find could be yours."
      }
      message="Be among the first to post an item for your campus."
      href="/listings/new"
      label="Post an item"
    />
  );
}
