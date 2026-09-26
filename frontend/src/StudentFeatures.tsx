import WantedReply from './WantedReply'
import { go } from './api'
import { useEffect, useState, type FormEvent } from 'react'
import { ProductCard } from './catalog'
import { EmptyState, Skeleton, toast, confirmAction } from './UI'

type Category = { id: number; categoryName: string }
type Wanted = { id: number; userId?: number; title: string; description: string; budget: number | null; categoryId: number | null; categoryName: string | null; status: string; studentName?: string; studentEmail?: string; createdAt: string }
type Saved = { productId: number; title: string; price: number | null; transactionType: string; status: string; sellerName: string; imageUrl: string | null }
type Form = { title: string; description: string; budget: string; categoryId: string; status: string }
const blank: Form = { title: '', description: '', budget: '', categoryId: '', status: 'Open' }

async function send<T>(url: string, token: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(url, { ...options, headers: { Authorization: `Bearer ${token}`, ...options.headers } })
  if (response.status === 204) return undefined as T
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.error ?? data.title ?? `Request failed (${response.status}).`)
  return data as T
}

export default function StudentFeatures({ token, path }: { token: string; path: string }) {
  const [reply,setReply]=useState<Wanted|null>(null)
  const [me,setMe]=useState<{id:number;role:string}|null>(null)
  useEffect(()=>{if(token)send<{id:number;role:string}>('/api/users/profile',token).then(setMe).catch(()=>{})},[token])
  const [view, setView] = useState<'wanted' | 'mine' | 'wishlist' | 'form'>(path==='/dashboard/wishlist'?'wishlist':path==='/dashboard/wanted'?'mine':path==='/wanted/new'?'form':'wanted')
  const [categories, setCategories] = useState<Category[]>([])
  const [posts, setPosts] = useState<Wanted[]>([])
  const [myPosts, setMyPosts] = useState<Wanted[]>([])
  const [saved, setSaved] = useState<Saved[]>([])
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [form, setForm] = useState<Form>(blank)
  const [editing, setEditing] = useState<number | null>(null)
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [reload, setReload] = useState(0)

  useEffect(() => {
    setPosts([]); setMyPosts([]); setSaved([]); setError(''); setView(path==='/dashboard/wishlist'?'wishlist':path==='/dashboard/wanted'?'mine':path==='/wanted/new'?'form':'wanted')
    if (!token) return
    let cancelled = false
    send<Category[]>('/api/products/categories', token).then(data => { if (!cancelled) setCategories(data) })
      .catch(e => { if (!cancelled) setError(e.message) })
    return () => { cancelled = true }
  }, [token])

  useEffect(() => {
    if (!token || view !== 'wanted') return
    let cancelled = false
    setLoading(true)
    const params = new URLSearchParams({ page: String(page) })
    if (search) params.set('search', search)
    if (categoryId) params.set('categoryId', categoryId)
    send<{ items: Wanted[]; total: number }>(`/api/wanted?${params}`, token)
      .then(data => { if (!cancelled) { setPosts(data.items); setTotal(data.total) } })
      .catch(e => { if (!cancelled) setError(e.message) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [token, view, search, categoryId, page, reload])

  useEffect(() => {
    if (!token || view !== 'mine') return
    send<Wanted[]>('/api/wanted/mine', token).then(setMyPosts).catch(e => setError(e.message))
  }, [token, view, reload])
  useEffect(() => {
    if (!token || view !== 'wishlist') return
    let cancelled = false
    setLoading(true)
    const refresh = () => send<Saved[]>('/api/wishlist', token).then(data => { if (!cancelled) setSaved(data) })
      .catch(e => { if (!cancelled) setError(e.message) }).finally(()=>{if(!cancelled)setLoading(false)})
    void refresh()
    window.addEventListener('premscart-wishlist-change', refresh)
    return () => { cancelled = true; window.removeEventListener('premscart-wishlist-change', refresh) }
  }, [token, view, reload])

  function startNew() { if(path!=='/wanted/new'){go('/wanted/new');return} setForm(blank); setEditing(null); setError(''); setNotice(''); setView('form') }
  function startEdit(post: Wanted) {
    setForm({ title: post.title, description: post.description, budget: post.budget?.toString() ?? '', categoryId: post.categoryId?.toString() ?? '', status: post.status })
    setEditing(post.id); setError(''); setNotice(''); setView('form')
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(''); setNotice('')
    const payload = { ...form, budget: form.budget ? Number(form.budget) : null, categoryId: form.categoryId ? Number(form.categoryId) : null }
    try {
      await send(editing ? `/api/wanted/${editing}` : '/api/wanted', token, {
        method: editing ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload),
      })
      go('/dashboard/wanted'); setReload(n => n + 1); setNotice(editing ? 'Request updated.' : 'Request posted.');toast(editing?'Request updated.':'Request posted.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not save request.') }
    finally { setBusy(false) }
  }
  async function removePost(post: Wanted) {
    if (!await confirmAction('Delete this request?',post.title,'Delete request',true)) return
    setBusy(true); setError('')
    try { await send<void>(`/api/wanted/${post.id}`, token, { method: 'DELETE' }); setReload(n => n + 1); setNotice('Request deleted.') }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not delete request.') }
    finally { setBusy(false) }
  }
  async function removeSaved(productId: number) {
    setBusy(true); setError('')
    try { await send<void>(`/api/wishlist/${productId}`, token, { method: 'DELETE' }); setSaved(items => items.filter(x => x.productId !== productId)); window.dispatchEvent(new Event('premscart-wishlist-change')) }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not remove saved item.') }
    finally { setBusy(false) }
  }
  function applySearch(event: FormEvent<HTMLFormElement>) { event.preventDefault(); setPage(1); setSearch(searchInput) }

  return <section className="student-features" id="student-features">
    <div className="section-heading"><div><p className="eyebrow">{view==='wishlist'?'SAVED FOR LATER':'STUDENT REQUESTS'}</p><h1>{view==='wishlist'?'Saved items':view==='mine'?'My requests':view==='form'?(editing?'Edit your request':'Ask for an item'):'What students need'}</h1><p>{view==='wishlist'?'Items you saved for later. Saving does not reserve them.':view==='mine'?'Update requests you posted and mark them fulfilled when you find what you need.':view==='form'?'Tell other students what you are looking for.':'Can you help? Reply to a request when you have a matching item to sell, rent, or give away.'}</p></div>{token && (view==='wanted'||view==='mine') && <button className="market-action" onClick={startNew}>+ Ask for an item</button>}</div>
    {!token ? <div className="empty-state"><h3>Student requests are for verified members</h3><p>Sign in to post what you need or save products for later.</p><a className="market-link" href="#account">Go to student sign in</a></div> : <>
      {notice && <p className="form-message" role="status">{notice}</p>}
      {error && <p className="form-error" role="alert">{error}</p>}
      {view === 'wanted' && <><form className="market-filters" onSubmit={applySearch}><label className="search-field">Search requests<input maxLength={100} placeholder="e.g., programming textbook" value={searchInput} onChange={e => setSearchInput(e.target.value)} /></label><button className="market-action" type="submit">Search</button><label>Category<select value={categoryId} onChange={e => { setPage(1); setCategoryId(e.target.value) }}><option value="">All categories</option>{categories.map(category => <option key={category.id} value={category.id}>{category.categoryName}</option>)}</select></label></form>{loading ? <Skeleton count={3} label="Loading requests"/> : posts.length === 0 ? <div className="empty-state"><h3>No requests found</h3><p>Post what you need and let other students know.</p></div> : <><p className="market-count">{total} open request{total === 1 ? '' : 's'}</p><div className="wanted-grid">{posts.map(post => <article className="wanted-card" key={post.id}><span className="type-pill">Looking for · {post.categoryName || 'Any category'}</span><h3>{post.title}</h3><p>{post.description}</p><strong>{post.budget == null ? 'Budget flexible' : `Budget up to ৳${post.budget.toLocaleString()}`}</strong><small>Requested by {post.studentName}</small><div className="wanted-actions">{post.userId===me?.id?<a className="button button-secondary" href="/dashboard/wanted">Manage your request</a>:me&&['Student','Business Seller'].includes(me.role)&&<button className="button button-primary" onClick={()=>setReply(post)}>I have this</button>}<a href={`/students/${post.userId}`}>View student</a></div></article>)}</div><div className="pagination"><button disabled={page <= 1} onClick={() => setPage(n => n - 1)}>Previous</button><span>Page {page} of {Math.ceil(total / 12)}</span><button disabled={page * 12 >= total} onClick={() => setPage(n => n + 1)}>Next</button></div></>}</>}
      {view === 'mine' && <div className="my-listings"><p className="helper-copy">Students send matching items to your Messages. After a successful handoff, edit the request and mark it Fulfilled.</p><a className="inline-link" href="/messages">View responses in Messages →</a>{myPosts.length === 0 ? <EmptyState title="You have no requests yet." message="Ask for something you need and other students can respond with a matching item." href="/wanted/new" label="Ask for an item" icon="wanted"/> : myPosts.map(post => <div className="my-listing" key={post.id}><span>{post.title} · {post.status} · {post.budget == null ? 'Flexible budget' : `৳${post.budget.toLocaleString()}`}</span><div><button onClick={() => startEdit(post)}>Edit</button><button disabled={busy} onClick={() => removePost(post)}>Delete</button></div></div>)}</div>}
      {view === 'wishlist' && <div className="my-listings">{loading?<Skeleton label="Loading saved items"/>:saved.length === 0 ? <EmptyState title="Save a few good finds." message="Tap the heart on any item to save it here. Saved items are not reserved." href="/marketplace" label="Find something you love" icon="heart"/> : <div className="catalog-grid">{saved.map(item => <ProductCard key={item.productId} product={{...item,id:item.productId}} token={token} saved onSave={()=>removeSaved(item.productId)} disabled={busy}/>)}</div>}</div>}
      {view === 'form' && <form className="listing-form" onSubmit={save}><div className="form-head"><span/><button className="text-button" type="button" onClick={() => go('/dashboard/wanted')}>Cancel</button></div><div className="form-grid"><label>What do you need?<input required maxLength={120} value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} /></label><label>Category<select value={form.categoryId} onChange={e => setForm(f => ({ ...f, categoryId: e.target.value }))}><option value="">Any category</option>{categories.map(c => <option key={c.id} value={c.id}>{c.categoryName}</option>)}</select></label><label>Maximum budget in ৳ (optional)<input type="number" min="0" max="9999999999" step="5" inputMode="numeric" value={form.budget} onChange={e => setForm(f => ({ ...f, budget: e.target.value }))} /></label>{editing&&<label>Status<select value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value }))}>{['Open', 'Fulfilled', 'Closed'].map(x => <option key={x}>{x}</option>)}</select></label>}<label className="wide-field">Details<textarea required maxLength={3000} rows={5} value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} /></label></div><button className="market-action" disabled={busy}>{busy ? 'Saving…' : editing ? 'Save changes' : 'Post request'}</button></form>}
    </>}
  {reply&&<WantedReply post={reply} onClose={()=>setReply(null)}/>}</section>
}
