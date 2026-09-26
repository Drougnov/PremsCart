import ReviewDialog from './ReviewDialog'
import { requestsChanged } from './RequestState'
import { go, api as request } from './api'
import OrderProgress, { nextStep } from './OrderProgress'
import { EmptyState, ErrorState, Skeleton, SafetyNote, toast, confirmAction } from './UI'
import { plusDays } from './rental'
import Icon from './Icon'
import { useEffect, useState, type FormEvent } from 'react'

type Listing = { id: number; title: string; price: number | null; transactionType: string; isNegotiable: boolean }
type Offer = { id: number; productId: number; productTitle: string; buyerId: number; buyerName: string; sellerId: number; sellerName: string; offerAmount: number; status: string; createdAt: string; lastProposerId: number; history: {authorId:number;amount:number;createdAt:string}[] }
type Order = {hasReviewed?:boolean; pickupLocationId?:number;rentalStartDate?:string;rentalDays?:number;rentalDueAt?:string;returnedAt?:string; id: number; productId: number; productTitle: string; transactionType: string; buyerId: number; buyerName: string; sellerId: number; sellerName: string; finalPrice: number | null; pickupLocation: string | null; pickupTime: string | null; pickupStatus: string; pickupProposerId: number | null; status: string; createdAt: string }
type Location = { id: number; locationName: string }

async function api<T>(url: string, token: string, body?: object): Promise<T> {
  const response = await fetch(url, { method: body ? 'POST' : 'GET', headers: { Authorization: `Bearer ${token}`, ...(body ? { 'Content-Type': 'application/json' } : {}) }, body: body ? JSON.stringify(body) : undefined })
  const result = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(result.error ?? result.title ?? `Request failed (${response.status})`)
  return result as T
}

export default function Transactions({ token, path }: { token: string; path: string }) {
  const [reviewOrder,setReviewOrder]=useState<Order|null>(null)
  const [scope,setScope]=useState<'active'|'history'|'all'>(new URLSearchParams(location.search).has('history')?'history':'active')
  const [query,setQuery]=useState('')
  const [now,setNow]=useState(Date.now())
  useEffect(()=>{const timer=setInterval(()=>setNow(Date.now()),1000);return()=>clearInterval(timer)},[])

  const orderId=path.startsWith('/orders/')?Number(path.split('/')[2]):null
  const [listing, setListing] = useState<Listing | null>(null)
  const [amount, setAmount] = useState('')
  const [offers, setOffers] = useState<Offer[]>([])
  const [orders, setOrders] = useState<Order[]>([])
  const [locations, setLocations] = useState<Location[]>([])
  const [me, setMe] = useState<number | null>(null)
  const [tab, setTab] = useState<'offers' | 'orders'>(path==='/dashboard/offers'?'offers':'orders')
  const [pickup, setPickup] = useState<Record<number, { locationId: string; date: string; time: string }>>({})
  const [counter, setCounter] = useState<Record<number, string>>({})
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  async function refresh() {
    if (!token) return
    const [receivedOffers, receivedOrders, receivedLocations, profile] = await Promise.all([
      api<Offer[]>('/api/transactions/offers', token), api<Order[]>('/api/transactions/orders', token),
      api<Location[]>('/api/transactions/locations', token), api<{ id: number }>('/api/users/profile', token),
    ])
    setOffers(receivedOffers); setOrders(receivedOrders); setLocations(receivedLocations); setMe(profile.id)
  }
  useEffect(() => {
    setListing(null); setOffers([]); setOrders([]); setLocations([]); setMe(null); setError(''); setNotice('')
    if (!token) return
    let active = true
    setLoading(true)
    Promise.all([api<Offer[]>('/api/transactions/offers', token), api<Order[]>('/api/transactions/orders', token), api<Location[]>('/api/transactions/locations', token), api<{ id: number }>('/api/users/profile', token)])
      .then(([o, r, l, p]) => { if (active) { setOffers(o); setOrders(r); setLocations(l); setMe(p.id) } })
      .catch(e => { if (active) setError(e.message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [token])
  useEffect(()=>{
    if(!token||busy)return
    let active=true
    const update=()=>{if(document.visibilityState==='visible')api<Order[]>('/api/transactions/orders',token).then(rows=>{if(active)setOrders(rows)}).catch(()=>{})}
    const timer=setInterval(update,15000)
    window.addEventListener('focus',update)
    return()=>{active=false;clearInterval(timer);window.removeEventListener('focus',update)}
  },[token,busy])
  useEffect(() => {
    const open = (event: Event) => {
      const product = (event as CustomEvent<Listing>).detail
      if (!token || !product?.id) return
      setListing(product); setAmount(product.price?.toString() ?? ''); setError(''); setNotice('')
      document.getElementById('transactions')?.scrollIntoView({ behavior: 'smooth' })
    }
    window.addEventListener('premscart-start-transaction', open)
    return () => window.removeEventListener('premscart-start-transaction', open)
  }, [token])
  async function act(url: string, body: object = {}, message = 'Updated.') {
    setBusy(true); setError(''); setNotice('')
    try { await api(url, token, body); await refresh();requestsChanged(); setNotice(message);toast(message) }
    catch (e) { setError(e instanceof Error ? e.message : 'Request failed.') }
    finally { setBusy(false) }
  }
  async function submit(event: FormEvent, offer: boolean) {
    event.preventDefault()
    if (!listing) return
    setBusy(true); setError(''); setNotice('')
    try {
      if (offer) await api('/api/transactions/offers', token, { productId: listing.id, amount: Number(amount) })
      else await api('/api/transactions/orders', token, { productId: listing.id })
      setListing(null); setTab(offer ? 'offers' : 'orders'); await refresh()
      setNotice(offer ? 'Offer sent to seller.' : 'Request sent to seller.');toast(offer?'Offer sent to seller.':'Request sent to seller.')
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not submit.') }
    finally { setBusy(false) }
  }
  const scopedOrders=orders.filter(o=>orderId?o.id===orderId:path==='/dashboard/sales'?o.sellerId===me:o.buyerId===me)
  const visibleOrders=scopedOrders.filter(o=>(orderId||scope==='all'||(scope==='history'?['Completed','Cancelled'].includes(o.status):!['Completed','Cancelled'].includes(o.status)))&&o.productTitle.toLowerCase().includes(query.toLowerCase()))
  async function chat(orderId:number){try{const c=await request(`/api/transactions/orders/${orderId}/conversation`,'POST',{});go(`/messages/${c.id}`)}catch(e){toast((e as Error).message,'error')}}
  const money = (value: number | null) => value == null ? 'Price arranged separately' : `Tk ${value.toLocaleString(undefined,{maximumFractionDigits:0})}`
  const pickupTimes=Array.from({length:25},(_,i)=>{const minutes=8*60+i*30;const h=String(Math.floor(minutes/60)).padStart(2,'0'),m=String(minutes%60).padStart(2,'0');return `${h}:${m}`})
  const dateValue=(d=new Date())=>`${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`
  const minPickupDate=dateValue()
  return <section id="transactions" className={`transactions-section ${orderId?'order-detail-page':''}`}>
    <div className="section-heading"><div><span className="eyebrow">{path==='/dashboard/offers'?'PRICE OFFERS':path==='/dashboard/sales'?'INCOMING REQUESTS':'MY ORDERS'}</span><h1>{orderId?`Order #${orderId}`:path==='/dashboard/offers'?'Price offers':path==='/dashboard/sales'?'Incoming requests':'My orders'}</h1><p>{orderId?'Review the order details and complete the next step here.':path==='/dashboard/offers'?'Track price negotiations in one place.':path==='/dashboard/sales'?'Review requests for items you are selling, renting, or giving away.':'Track items you are buying, renting, or receiving through giveaways.'}</p></div></div>
    {!token ? <p className="feature-empty">Sign in to make an offer or view your orders.</p> : <>
      {error && <ErrorState message={error} retry={()=>{setError('');setLoading(true);void refresh().catch(e=>setError(e.message)).finally(()=>setLoading(false))}}/>}{notice && <p role="status" className="form-success">{notice}</p>}
      {listing && <form className="transaction-compose" onSubmit={e => submit(e, false)}><div><h3>{listing.title}</h3><p>{listing.transactionType==='Sell'?'For sale':listing.transactionType==='Rent'?'Rent':'Giveaway'} · {money(listing.price)}</p></div>
        <div className="transaction-buttons"><button className="market-action" disabled={busy} type="submit">Request this item</button><button type="button" onClick={() => setListing(null)}>Close</button></div>
        {listing.transactionType === 'Sell' && <div className="offer-compose"><label>{listing.isNegotiable ? 'Your offer in ৳' : 'Fixed price in ৳'}<input type="number" min="5" step="5" inputMode="numeric" required value={amount} disabled={!listing.isNegotiable || busy} onChange={e => setAmount(e.target.value)} /></label><button type="button" disabled={busy || !amount} onClick={e => void submit(e, true)}>Send offer</button></div>}
      </form>}
      <div className="order-page-navigation">{orderId?<a className="inline-link" href={scopedOrders[0]?.sellerId===me?'/dashboard/sales':'/dashboard/purchases'}>← Back to {scopedOrders[0]?.sellerId===me?'incoming requests':'my orders'}</a>:<div className="feature-tabs"><button className={tab==='offers'?'active':''} onClick={()=>go('/dashboard/offers')}>Price offers ({offers.length})</button><button className={tab==='orders'&&path!=='/dashboard/sales'?'active':''} onClick={()=>go('/dashboard/purchases')}>My orders</button><button className={path==='/dashboard/sales'?'active':''} onClick={()=>go('/dashboard/sales')}>Incoming requests</button></div>}</div>
      {tab==='orders'&&!orderId&&<div className="order-toolbar"><div className="catalog-tabs" role="group" aria-label="Order history filter">{(['active','history','all'] as const).map(x=><button key={x} className={scope===x?'active':''} aria-pressed={scope===x} onClick={()=>setScope(x)}>{x==='active'?'Active':x==='history'?'Completed & cancelled':'All orders'}</button>)}</div><label className="search-field"><span className="sr-only">Search orders</span><input placeholder="Search by item name" value={query} onChange={e=>setQuery(e.target.value)}/></label></div>}
      {loading ? <Skeleton count={2} label="Loading orders"/> : tab === 'offers' ? offers.length ? <div className="transaction-list">{offers.map(o => <article className="transaction-card" key={o.id}><div><span className="type-pill">{o.status}</span><h3>{o.productTitle}</h3><p>{o.buyerId === me ? `To ${o.sellerName}` : `From ${o.buyerName}`} · {money(o.offerAmount)}</p><small>{new Date(o.createdAt).toLocaleString()}</small>{'history' in o && <ol className="offer-history">{o.history.map((h,i)=><li key={i}>{h.authorId===me?'You':h.authorId===o.buyerId?o.buyerName:o.sellerName}: ৳{h.amount} · {new Date(h.createdAt).toLocaleString()}</li>)}</ol>}</div><div className="transaction-buttons">
        {['Pending','Countered'].includes(o.status) && o.lastProposerId !== me && <><button disabled={busy} className="market-action" onClick={() => act(`/api/transactions/offers/${o.id}/accept`, {}, 'Offer accepted; order created.')}>Accept</button><button disabled={busy} onClick={() => act(`/api/transactions/offers/${o.id}/reject`, {}, 'Offer declined.')}>Decline</button><label>Counter price in ৳<input type="number" min="5" step="5" inputMode="numeric" value={counter[o.id] ?? ''} onChange={e => setCounter(v => ({ ...v, [o.id]: e.target.value }))} /></label><button disabled={busy || !counter[o.id]} onClick={() => act(`/api/transactions/offers/${o.id}/counter`, { amount: Number(counter[o.id]) }, 'Counteroffer sent.')}>Counter</button></>}
        
      {['Pending','Countered'].includes(o.status)&&o.buyerId===me&&<button disabled={busy} onClick={()=>act(`/api/transactions/offers/${o.id}/withdraw`,{},'Offer withdrawn.')}>Withdraw</button>}</div></article>)}</div> : <EmptyState title="No offers yet." message="Find an item marked open to price offers. Your price conversation will appear here." href="/marketplace?negotiable=true" label="Find items open to offers" icon="offer"/> : visibleOrders.length ? <div className="transaction-list">{visibleOrders.map(o => <article className="transaction-card order-card" key={o.id}><div className="order-card-body"><div className="order-card-title"><span className={`listing-badge ${o.transactionType.toLowerCase()}`}>{o.transactionType==='Sell'?'For sale':o.transactionType}</span><a href={`/orders/${o.id}`}>Order #{o.id}</a></div><h3><a href={`/orders/${o.id}`}>{o.productTitle}</a></h3><OrderProgress order={o}/><p className="order-next-step"><Icon name="arrow"/>{nextStep(o,me,now)}</p><p>{o.transactionType==='Sell'?'For sale':o.transactionType} · {money(o.finalPrice)} · {o.buyerId === me ? `Seller: ${o.sellerName}` : `Buyer: ${o.buyerName}`}</p>{o.rentalStartDate&&<p className="preferred-dates">Preferred rental: {o.rentalStartDate} → {plusDays(o.rentalStartDate,o.rentalDays??1)} · Confirm the exact handoff in chat.</p>}{orderId&&o.pickupLocation&&<section className="pickup-summary" aria-label="Pickup arrangement"><div className="pickup-summary-heading"><Icon name="clock"/><strong>{o.pickupStatus==='Completed'?'Pickup completed':o.pickupStatus==='Confirmed'?'Pickup agreed':'Pickup proposal'}</strong><span className="status-badge">{o.pickupStatus==='Proposed'?'Awaiting agreement':o.pickupStatus}</span></div><dl><div><dt>Meet at</dt><dd>{o.pickupLocation}</dd></div><div><dt>Date & time</dt><dd>{o.pickupTime&&new Date(o.pickupTime).toLocaleString([], {dateStyle:'medium',timeStyle:'short'})}</dd></div></dl>{o.pickupStatus==='Proposed'&&o.status==='Pickup scheduled'&&<><p>{o.pickupProposerId===me?'You proposed this pickup. Waiting for the other person to agree.':o.pickupTime&&Date.parse(o.pickupTime)<=now?'This proposal’s time has passed. Suggest a new pickup time below.':'The other person proposed this pickup. Agree below or suggest a different place and time.'}</p>{o.pickupProposerId!==me&&!!o.pickupTime&&Date.parse(o.pickupTime)>now&&<button className="button button-primary" disabled={busy} onClick={()=>act(`/api/transactions/orders/${o.id}/pickup/confirm`,{},'Pickup agreed. Meet at the scheduled time.')}>Agree to this pickup</button>}</>}{o.pickupStatus==='Confirmed'&&o.status==='Pickup scheduled'&&<p>{o.pickupTime&&Date.parse(o.pickupTime)>now?'Your pickup is agreed. Handoff confirmation becomes available to the buyer at the scheduled time.':'The scheduled time has arrived. The buyer should confirm only after receiving the item.'}</p>}</section>}<small>{new Date(o.createdAt).toLocaleString()}</small></div><div className="transaction-buttons">
        {!orderId ? <a className="button button-primary" href={`/orders/${o.id}`}>Open order <Icon name="arrow"/></a> : <>
        {o.status === 'Pending' && o.sellerId === me && <button className="market-action" disabled={busy} onClick={() => act(`/api/transactions/orders/${o.id}/accept`, {}, 'Request accepted.')}>Accept request</button>}
        {(o.status === 'Accepted' || o.status === 'Pickup scheduled') && <details className="pickup-arrangement"><summary>{o.pickupTime?'Suggest a different pickup':'Propose a pickup'} <Icon name="chevron"/></summary><p className="helper-copy">A new proposal replaces the previous agreement. The other person must agree again.</p><div className="pickup-controls"><label>Meet at<select value={pickup[o.id]?.locationId ?? ''} onChange={e => setPickup(v => ({ ...v, [o.id]: { locationId: e.target.value, date: v[o.id]?.date ?? '', time: v[o.id]?.time ?? '' } }))}><option value="">Choose a campus place</option>{locations.map(l => <option key={l.id} value={l.id}>{l.locationName}</option>)}</select></label><label>Pickup date<input type="date" min={minPickupDate} value={pickup[o.id]?.date ?? ''} onChange={e => setPickup(v => ({ ...v, [o.id]: { locationId: v[o.id]?.locationId ?? '', date: e.target.value, time: v[o.id]?.time ?? '' } }))} /></label><label>Pickup time<select value={pickup[o.id]?.time ?? ''} onChange={e => setPickup(v => ({ ...v, [o.id]: { locationId: v[o.id]?.locationId ?? '', date: v[o.id]?.date ?? '', time: e.target.value } }))}><option value="">Choose a time</option>{pickupTimes.map(t=><option key={t} value={t}>{new Date(`2000-01-01T${t}`).toLocaleTimeString([], {hour:'numeric',minute:'2-digit'})}</option>)}</select></label><button className="button button-primary" disabled={busy || !pickup[o.id]?.locationId || !pickup[o.id]?.date || !pickup[o.id]?.time} onClick={() => act(`/api/transactions/orders/${o.id}/pickup`, { locationId: Number(pickup[o.id].locationId), pickupTime: new Date(`${pickup[o.id].date}T${pickup[o.id].time}`).toISOString() }, 'Pickup proposed. Waiting for the other person to agree.')}>Send pickup proposal</button></div><SafetyNote/></details>}
        {o.status === 'Pickup scheduled' && o.pickupStatus === 'Confirmed' && o.buyerId === me && !!o.pickupTime && Date.parse(o.pickupTime)<=now && <button className="market-action" disabled={busy} onClick={() => act(`/api/transactions/orders/${o.id}/complete`, {}, o.transactionType==='Rent'?'Pickup confirmed. Your rental has started.':'Pickup confirmed. Order completed.')}>{o.transactionType==='Rent'?'Confirm pickup · start rental':'Confirm item received'}</button>}
        {o.rentalDueAt&&<div className="rental-status"><strong>{o.rentalDays}-day rental</strong><p>Return by {new Date(o.rentalDueAt).toLocaleString()}</p>{['Rented','Return requested'].includes(o.status)&&new Date(o.rentalDueAt)<new Date()&&<span className="form-error">Overdue — please coordinate the return.</span>}</div>}{o.status==='Rented'&&o.buyerId===me&&<button disabled={busy} onClick={()=>act(`/api/transactions/orders/${o.id}/return`,{},'Return requested. Arrange the handoff with the owner.')}>I’m ready to return it</button>}{o.status==='Return requested'&&o.sellerId===me&&<button disabled={busy} onClick={async()=>{if(await confirmAction('Confirm the return?','Confirm only after you have received and inspected the rental item.','Confirm item returned'))void act(`/api/transactions/orders/${o.id}/return/confirm`,{},'Return confirmed. Rental completed.')}}>Confirm item returned</button>}{o.status === 'Completed' && <button disabled={o.hasReviewed} onClick={()=>setReviewOrder(o)}>{o.hasReviewed?'Review submitted':`Review ${o.buyerId===me?'seller':'buyer'}`}</button>}
        <button disabled={busy} onClick={()=>chat(o.id)}><Icon name="message"/>Open conversation</button>{['Pending', 'Accepted', 'Pickup scheduled'].includes(o.status) && <button disabled={busy} onClick={async() => {if(await confirmAction('Cancel this request?','The other student will be notified. Any reservation for this request will be released.','Cancel request',true))await act(`/api/transactions/orders/${o.id}/cancel`, {}, 'Order cancelled.')}}>Cancel order</button>}
        </>}
      </div></article>)}</div> : <EmptyState title={orderId?'Order not found.':scope==='history'?'Your completed orders will live here.':'Nothing waiting on you right now.'} message={orderId?'This order may belong to another account.':'Browse items, or switch to All orders to see previous orders.'} href="/marketplace" label="Browse items" icon="purchase"/>}
    </>}
  {reviewOrder&&<ReviewDialog order={reviewOrder} onClose={()=>setReviewOrder(null)} onSaved={()=>{setOrders(rows=>rows.map(o=>o.id===reviewOrder.id?{...o,hasReviewed:true}:o));requestsChanged()}}/>}</section>
}
