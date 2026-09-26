import { useEffect, useState } from 'react'
import { api } from './api'
import { ProductCard, useWishlist, type Product } from './catalog'
import { EmptyState, ErrorState, Modal, Skeleton, toast, confirmAction } from './UI'
import Icon from './Icon'
import ProductDetail from './ProductDetail'
import ListingForm from './ListingForm'
export { PrivateImage } from './Media'

type Category = { id: number; categoryName: string }
type Filters = {
  search: string
  categoryId: string
  type: string
  minPrice: string
  maxPrice: string
  condition: string
  sort: string
  negotiable: string
}

const empty: Filters = { search:'', categoryId:'', type:'', minPrice:'', maxPrice:'', condition:'', sort:'newest', negotiable:'' }
const fixedType = (path: string) => path === '/rentals' ? 'Rent' : path === '/giveaways' ? 'Giveaway' : ''

function initialFilters(path: string): Filters {
  const q = new URLSearchParams(location.search)
  const result = { ...empty } as Filters
  for (const key of Object.keys(empty) as (keyof Filters)[]) result[key] = q.get(key) ?? empty[key]
  const type = fixedType(path)
  if (type) result.type = type
  return result
}

export default function Marketplace({ token, path }: { token: string; path: string }) {
  const detail = /^\/listings\/(\d+)$/.exec(path)
  const editing = /^\/listings\/(\d+)\/edit$/.exec(path)
  const mine = path === '/dashboard/listings'
  const [filters,setFilters] = useState<Filters>(() => initialFilters(path))
  const [draft,setDraft] = useState<Filters>(() => initialFilters(path))
  const [filterOpen,setFilterOpen] = useState(false)
  const [page,setPage] = useState(() => Math.max(1, Number(new URLSearchParams(location.search).get('page')) || 1))
  const [version,setVersion] = useState(0)
  const [items,setItems] = useState<Product[]>([])
  const [total,setTotal] = useState(0)
  const [categories,setCategories] = useState<Category[]>([])
  const [profile,setProfile] = useState<{id:number;role:string}|null>(null)
  const [error,setError] = useState('')
  const [filterError,setFilterError] = useState('')
  const [loading,setLoading] = useState(true)
  const [mineFilter,setMineFilter] = useState('All')
  const wishlist = useWishlist()
  const browse = !detail && !editing && path !== '/listings/new' && !mine

  useEffect(() => {
    let active = true
    Promise.all([api<Category[]>('/api/products/categories'), api<{id:number;role:string}>('/api/users/profile')])
      .then(([c,p]) => { if (active) { setCategories(c); setProfile(p) } })
      .catch(e => { if (active) setError(e.message) })
    return () => { active = false }
  }, [])

  useEffect(() => {
    if (!browse && !mine) { setLoading(false); return }
    let active = true
    setLoading(true); setError('')
    const q = new URLSearchParams({ page:String(page) })
    ;(Object.entries(filters) as [keyof Filters,string][]).forEach(([k,v]) => { if (v) q.set(k,v) })
    api(mine ? '/api/products/mine' : `/api/products?${q}`)
      .then(d => { if (active) { setItems(mine ? d : d.items); setTotal(mine ? d.length : d.total) } })
      .catch(e => { if (active) setError(e.message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [browse,mine,filters,page,version])

  function commit(next: Filters, newPage = 1) {
    const type = fixedType(path)
    const safe = type ? { ...next, type } : next
    setFilters(safe); setDraft(safe); setPage(newPage)
    const q = new URLSearchParams()
    ;(Object.entries(safe) as [keyof Filters,string][]).forEach(([k,v]) => {
      if (!v || (k === 'sort' && v === 'newest') || (k === 'type' && type)) return
      q.set(k,v)
    })
    if (newPage > 1) q.set('page',String(newPage))
    history.replaceState({},'',path + (q.size ? `?${q}` : ''))
  }

  function apply() {
    if (draft.minPrice && draft.maxPrice && Number(draft.minPrice) > Number(draft.maxPrice)) {
      setFilterError('Minimum price must be lower than the maximum price.'); return
    }
    setFilterError(''); commit(draft); setFilterOpen(false)
  }

  function clear() { commit({ ...empty, type: fixedType(path) }); setFilterError('') }

  const labels: Partial<Record<keyof Filters,string>> = { search:'Search', categoryId:'Category', type:'Offer type', minPrice:'Minimum', maxPrice:'Maximum', condition:'Condition', negotiable:'Price offers' }
  const active = (Object.entries(filters) as [keyof Filters,string][]).filter(([k,v]) => v && k !== 'sort' && !(k === 'type' && !!fixedType(path)))
  const displayFilterValue = (key:string,value:string) => key==='categoryId'
    ? categories.find(c => String(c.id)===value)?.categoryName??value
    : key==='type' ? (value==='Sell'?'For sale':value==='Rent'?'Rent':'Giveaway')
      : key==='negotiable' ? 'Open to offers' : value

  async function remove(p: Product) {
    if (!await confirmAction('Delete this item?', `“${p.title}” will be removed. Items with order or message history need to stay for your records.`, 'Delete item', true)) return
    try { await api(`/api/products/${p.id}`,'DELETE'); toast('Item deleted.'); setVersion(v => v + 1) }
    catch (e) { toast((e as Error).message,'error') }
  }

  const filterFields = <>
    {path === '/marketplace' && <label>Offer type
      <select value={draft.type} onChange={e => setDraft(d => ({...d,type:e.target.value,minPrice:e.target.value==='Giveaway'?'':d.minPrice,maxPrice:e.target.value==='Giveaway'?'':d.maxPrice,negotiable:e.target.value&&e.target.value!=='Sell'?'':d.negotiable}))}>
        <option value="">All items</option><option value="Sell">For sale</option><option value="Rent">Rent</option><option value="Giveaway">Giveaway</option>
      </select>
    </label>}
    <label>Category
      <select value={draft.categoryId} onChange={e => setDraft(d => ({...d,categoryId:e.target.value}))}>
        <option value="">All categories</option>{categories.map(c => <option key={c.id} value={c.id}>{c.categoryName}</option>)}
      </select>
    </label>
    {path !== '/giveaways' && draft.type !== 'Giveaway' && <div className="price-range">
      <label>Minimum price (Tk)<input type="number" min="0" step="5" inputMode="numeric" value={draft.minPrice} onChange={e => setDraft(d => ({...d,minPrice:e.target.value}))}/></label>
      <label>Maximum price (Tk)<input type="number" min="0" step="5" inputMode="numeric" value={draft.maxPrice} onChange={e => setDraft(d => ({...d,maxPrice:e.target.value}))}/></label>
    </div>}
    {(path === '/rentals' || draft.type === 'Rent') && <p className="helper-copy">Price means the daily rental rate.</p>}
    <label>Condition
      <select value={draft.condition} onChange={e => setDraft(d => ({...d,condition:e.target.value}))}>
        <option value="">Any condition</option>{['New','Like new','Good','Fair'].map(x => <option key={x}>{x}</option>)}
      </select>
    </label>
    {path === '/marketplace' && (!draft.type || draft.type === 'Sell') && <label className="check-field"><input type="checkbox" checked={!!draft.negotiable} onChange={e => setDraft(d => ({...d,negotiable:e.target.checked?'true':''}))}/>Only show sale items open to price offers</label>}
  </>

  if (detail) return <ProductDetail id={Number(detail[1])} token={token} profile={profile}/>
  if (editing || path === '/listings/new') return <ListingForm id={editing ? Number(editing[1]) : undefined} token={token} profile={profile} categories={categories}/>

  const visible = mine ? items.filter(p => mineFilter === 'All' || (mineFilter === 'Hidden' ? p.isHidden : p.status === mineFilter)) : items
  const pageCopy = path === '/rentals'
    ? { eyebrow:'RENT ON CAMPUS', title:'Rent what you need.', text:'A focused rental view of the same campus shop. Choose your dates from the item page.' }
    : path === '/giveaways'
      ? { eyebrow:'GIVEAWAYS ON CAMPUS', title:'Find giveaway items.', text:'Items students are passing on at no cost. Request one and arrange a campus pickup.' }
      : { eyebrow:'CAMPUS SHOP', title:'Find what you need.', text:'Search campus items in one place, then filter by For sale, Rent, or Giveaway.' }

  return <section className="page-section marketplace-page">
    <div className="page-heading">
      <div><span className="eyebrow">{mine?'MY ITEMS':pageCopy.eyebrow}</span><h1>{mine?'Items you posted':pageCopy.title}</h1><p>{mine?'Edit your posts and see whether each item is available, reserved, sold, given away, or hidden.':pageCopy.text}</p></div>
    </div>

    {browse && <>
      <div className="discovery-toolbar">
        <form className="discovery-search" onSubmit={e => { e.preventDefault(); commit({...filters,search:draft.search}) }}>
          <Icon name="search"/><label className="sr-only" htmlFor="item-search">Search items</label>
          <input id="item-search" value={draft.search} maxLength={100} placeholder={path==='/rentals'?'Search rentals…':path==='/giveaways'?'Search giveaways…':'Search books, electronics, clothes and more…'} onChange={e => setDraft(d => ({...d,search:e.target.value}))}/>
          <button className="button button-primary" type="submit">Search</button>
        </form>
        <button className="button button-secondary" onClick={() => { setDraft(filters); setFilterError(''); setFilterOpen(true) }}><Icon name="settings"/>Filters{active.length?` (${active.length})`:''}</button>
        <label className="sort-control"><span className="sr-only">Sort items</span><select aria-label="Sort items" value={filters.sort} onChange={e => commit({...filters,sort:e.target.value})}><option value="newest">Newest first</option><option value="price-low">Lowest price first</option><option value="price-high">Highest price first</option></select></label>
      </div>
      <div className="filter-chips">{active.map(([k,v]) => <button key={k} onClick={() => commit({...filters,[k]:''})}>{labels[k]??k}: {displayFilterValue(k,v)}<Icon name="close"/></button>)}{active.length>0 && <button className="text-button" onClick={clear}>Clear filters</button>}</div>
    </>}

    {mine && <div className="catalog-tabs" role="group" aria-label="Filter my items">{['All','Available','Reserved','Sold','GivenAway','Unavailable','Hidden'].map(s => <button key={s} aria-pressed={mineFilter===s} className={mineFilter===s?'active':''} onClick={() => setMineFilter(s)}>{s==='GivenAway'?'Given away':s}</button>)}</div>}

    {error ? <ErrorState message={error} retry={() => setVersion(v => v + 1)}/> : loading ? <Skeleton/> : visible.length ? <>
      <div className="results-meta"><span>{mine?visible.length:total} item{(mine?visible.length:total)===1?'':'s'}</span>{!mine && <span>Prices in Bangladeshi taka · Pickup arranged after a request is accepted</span>}</div>
      <div className="catalog-grid">{visible.map(p => mine ? <div key={p.id} className="owned-listing"><ProductCard product={p} token={token}/><span className="status-badge">{p.isHidden?'Hidden by administration':p.status}</span><div className="row-actions"><a className="button button-secondary compact" href={`/listings/${p.id}/edit`}>Edit item</a><button className="text-button danger-text" onClick={() => remove(p)}>Delete</button></div></div> : <ProductCard key={p.id} product={p} token={token} saved={wishlist.saved.includes(p.id)} disabled={wishlist.pending.includes(p.id)} onSave={() => wishlist.toggle(p.id)}/>)}</div>
      {!mine && total > 12 && <nav className="pagination" aria-label="Item pages"><button disabled={page<=1} onClick={() => { commit(filters,page-1); window.scrollTo(0,0) }}>Previous</button><span>Page {page} of {Math.ceil(total/12)}</span><button disabled={page*12>=total} onClick={() => { commit(filters,page+1); window.scrollTo(0,0) }}>Next</button></nav>}
    </> : <EmptyState title={mine?'You have not posted anything yet.':'No items match those filters.'} message={mine?'Post something you want to sell, rent, or give away.':'Try another search, clear the filters, or create a request if you cannot find what you need.'} href={mine?'/listings/new':'/wanted/new'} label={mine?'Post an item':'Ask for an item'} action={!mine?clear:undefined} actionLabel="Clear filters" icon={mine?'listing':'search'}/>}

    <Modal open={filterOpen} title="Filter shop items" onClose={() => setFilterOpen(false)}><form className="filter-drawer-form" onSubmit={e => { e.preventDefault(); apply() }}>{filterError && <p className="form-error" role="alert">{filterError}</p>}{filterFields}<div className="modal-actions"><button type="button" onClick={() => { setDraft({...empty,type:fixedType(path)}); setFilterError('') }}>Reset</button><button className="button button-primary">Show items</button></div></form></Modal>
  </section>
}
