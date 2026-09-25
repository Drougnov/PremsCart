import { useEffect, useState, type FormEvent } from 'react'
import { api, go, signout } from './api'
import { PrivateImage } from './Marketplace'
import { EmptyState, ErrorState, Skeleton, toast } from './UI'
import Icon, { type IconName } from './Icon'

export function useLoad(path: string | null) {
  const [data, setData] = useState<any>(null), [error, setError] = useState('')
  const [version, setVersion] = useState(0)
  useEffect(() => { let active = true; setData(null); setError(''); if(!path)return; api(path).then(d => { if(active) setData(d) }).catch(e => { if(active) setError(e.message) }); return () => { active=false } }, [path,version])
  return { data, error, refresh: () => setVersion(v => v+1) }
}

export function State({error, data}: {error: string; data: any}) {
  return error ? <p className="form-error" role="alert">{error}</p> : !data ? <div className="loading-state" role="status"><span className="loading-spinner"/>Loading…</div> : null
}

const metricIcons: Record<string, IconName> = {
  activeListings:'listing', sold:'sale', givenAway:'gift', pendingPurchases:'purchase', unreadMessages:'message',
  notifications:'bell', wishlist:'heart', rating:'star', completed:'package', users:'user', listings:'market', transactions:'purchase', stores:'store', pendingReports:'shield'
}
export function Metrics({data}: {data: any}) {
  return <div className="metrics">{Object.entries(data).filter(([,v])=>typeof v==='number').map(([k,v])=><article className="metric-card" key={k}><span className="metric-icon"><Icon name={metricIcons[k]??'dashboard'}/></span><div><small>{k.replace(/([A-Z])/g,' $1')}</small><strong>{Number(v).toLocaleString(undefined,{maximumFractionDigits:1})}</strong></div></article>)}</div>
}

export function Dashboard() {
  const {data,error}=useLoad('/api/dashboard/student')
  return <section className="page-section dashboard-page">
    <div className="page-heading dashboard-heading"><div><span className="eyebrow">MY PREMSCART</span><h1>Your campus dashboard</h1><p>Listings, orders, messages and pickups — organized in one place.</p></div><div className="dashboard-heading-actions"><a className="button button-primary" href="/listings/new"><Icon name="plus"/> Post a listing</a><a className="button button-secondary" href="/wanted/new"><Icon name="wanted"/> Post a wanted request</a></div></div>
    <State data={data} error={error}/>
    {data&&<>
      <Metrics data={data}/>
      <div className="dashboard-content-grid">
        <section className="content-panel"><div className="panel-heading"><div><span className="eyebrow">NEXT UP</span><h2>Upcoming pickups</h2></div><a className="inline-link" href="/dashboard/purchases">View orders <Icon name="arrow"/></a></div>{data.pickups.length ? <div className="compact-list">{data.pickups.map((p:any)=><article className="pickup-row" key={p.id}><span className="row-icon"><Icon name="package"/></span><div><h3>{p.title}</h3><p>{new Date(p.pickupTime).toLocaleString()}</p></div><span className="status-badge">{p.pickupStatus}</span></article>)}</div>:<div className="panel-empty"><Icon name="package"/><div><strong>No pickups arranged yet</strong><span>Your next scheduled handoff will appear here.</span></div></div>}</section>
        <aside className="content-panel quick-panel"><div className="panel-heading"><div><span className="eyebrow">SHORTCUTS</span><h2>Quick actions</h2></div></div><a href="/dashboard/listings"><span><Icon name="listing"/></span><div><strong>Manage listings</strong><small>Edit, update or remove your posts</small></div><Icon name="chevron"/></a><a href="/messages"><span><Icon name="message"/></span><div><strong>Open messages</strong><small>Continue campus conversations</small></div><Icon name="chevron"/></a><a href="/dashboard/wishlist"><span><Icon name="heart"/></span><div><strong>Saved items</strong><small>Return to products you liked</small></div><Icon name="chevron"/></a></aside>
      </div>
    </>}
  </section>
}

export function Notifications() {
  const {data,error,refresh}=useLoad('/api/notifications'); const [issue,setIssue]=useState(''),[unreadOnly,setUnreadOnly]=useState(false),[busy,setBusy]=useState(false)
  async function read(id?:number){setBusy(true);setIssue('');try{await api(id?`/api/notifications/${id}/read`:'/api/notifications/read-all','POST',{});refresh();window.dispatchEvent(new Event('premscart-notifications-change'));return true}catch(e){setIssue((e as Error).message);return false}finally{setBusy(false)}}
  const rows=data?.items.filter((n:any)=>!unreadOnly||!n.isRead)??[]
  return <section className="page-section"><div className="page-heading"><div><span className="eyebrow">YOUR CAMPUS ACTIVITY</span><h1>Stay in the loop.</h1><p>Messages, offers, pickups, and the little things along the way.</p></div><a className="button button-secondary" href="/settings/notifications">Notification settings</a></div>{(error||issue)&&<ErrorState message={error||issue} retry={refresh}/>}<div className="management-toolbar"><div className="catalog-tabs"><button className={!unreadOnly?'active':''} onClick={()=>setUnreadOnly(false)}>All updates</button><button className={unreadOnly?'active':''} onClick={()=>setUnreadOnly(true)}>Unread {data?.unread??0}</button></div>{data?.unread>0&&<button disabled={busy} onClick={async()=>{if(await read())toast('All notifications marked as read.')}}>Mark all read</button>}</div>{!data&&!error?<Skeleton count={2} label="Loading notifications"/>:data&&<div className="notification-list">{!rows.length&&<EmptyState title="You’re all caught up." message="New updates will appear here when someone connects with you." href="/marketplace" label="Explore campus" icon="bell"/>}{rows.map((n:any)=><article className={`notification-row ${n.isRead?'':'unread'}`} key={n.id}><span className="row-icon"><Icon name="bell"/></span><div><h3>{n.title}</h3><p>{n.message}</p><small>{new Date(n.createdAt).toLocaleString()}</small></div><button className="button button-secondary compact" disabled={busy} onClick={async()=>{if(await read(n.id))go(n.link)}}>View update</button></article>)}</div>}</section>
}

export function Security({recovery=false}:{recovery?:boolean}) {
  const [message,setMessage]=useState(''), [error,setError]=useState(''),[busy,setBusy]=useState(false)
  async function submit(e:FormEvent<HTMLFormElement>){e.preventDefault();setBusy(true);setError('');const f=new FormData(e.currentTarget);const action=String(f.get('action')||'change-password');try{const r=await api(`/api/auth/${action}`,'POST',Object.fromEntries(f));setMessage(r.message);if(action==='change-password')signout()}catch(e){setError((e as Error).message)}finally{setBusy(false)}}
  return <section className="feature-section narrow security-card"><span className="gate-icon"><Icon name="lock"/></span><span className="eyebrow">ACCOUNT SECURITY</span><h1>{recovery?'Recover your account':'Password & security'}</h1><p>{recovery?'Request a reset code or finish resetting your password.':'Choose a strong password and keep your university account protected.'}</p>{error&&<p className="form-error" role="alert">{error}</p>}{message&&<p className="form-success" role="status">{message}</p>}{recovery&&<form onSubmit={submit}><input type="hidden" name="action" value="forgot-password"/><label>University email<input name="email" type="email" required/></label><button className="button button-secondary" disabled={busy}>Send reset code</button></form>}<form onSubmit={submit}><input type="hidden" name="action" value={recovery?'reset-password':'change-password'}/>{recovery?<><label>University email<input name="email" type="email" required/></label><label>Reset code<input name="code" pattern="[0-9]{6}" required/></label></>:<label>Current password<input name="currentPassword" type="password" required/></label>}<label>New password<input name="password" type="password" minLength={8} maxLength={128} required/></label><button className="button button-primary" disabled={busy}>{recovery?'Reset password':'Change password'}</button></form></section>
}

export function PhotoUpload({store=false}:{store?:boolean}) {
  const [message,setMessage]=useState(''),[busy,setBusy]=useState(false)
  return <form className="photo-upload" onSubmit={async e=>{e.preventDefault();setBusy(true);try{await api(store?'/api/stores/mine/logo':'/api/users/profile/image','POST',new FormData(e.currentTarget));setMessage('Image saved.')}catch(e){setMessage((e as Error).message)}finally{setBusy(false)}}}><div><span className="eyebrow">IMAGE</span><h3>{store?'Store logo':'Profile picture'}</h3></div><label>Select image<input type="file" name="file" accept="image/jpeg,image/png,image/webp" required/></label><button className="button button-primary" disabled={busy}>Upload image</button>{message&&<p role="status">{message}</p>}</form>
}

export function StudentProfile({id,token}:{id:number;token:string}) {
  const {data,error}=useLoad(`/api/users/${id}`), rep=useLoad(`/api/community/reputation/${id}`)
  return <section className="page-section profile-page"><State data={data} error={error}/>{data&&<><div className="profile-heading"><div className="profile-photo"><PrivateImage url={data.profile.profileImage} token={token} alt="Profile picture"/></div><div><span className="verified-pill"><Icon name="shield"/> University email verified</span><h1>{data.profile.firstName} {data.profile.lastName}</h1><p>{data.profile.department} · Batch {data.profile.batch}</p></div></div><Metrics data={{completed:data.completed,sold:data.sold,givenAway:data.givenAway,rating:rep.data?.averageRating??0}}/><section className="content-panel"><div className="panel-heading"><h2>Active listings</h2></div>{data.listings.length?<div className="compact-list">{data.listings.map((p:any)=><article className="simple-row" key={p.id}><a href={`/listings/${p.id}`}>{p.title}</a><strong>{p.transactionType==='Giveaway'?'Free':`৳${p.price}`}</strong></article>)}</div>:<p className="feature-empty">No active listings.</p>}</section><section className="content-panel"><div className="panel-heading"><h2>Student reviews</h2></div><State data={rep.data} error={rep.error}/>{rep.data?.reviews.map((r:any)=><article className="review-row" key={r.id}><div><h3><span className="rating-stars">★</span> {r.rating} / 5 · {r.reviewerName}</h3><p>{r.comment}</p></div><button className="text-button danger-text" onClick={()=>window.dispatchEvent(new CustomEvent('premscart-report',{detail:{reviewId:r.id,name:'Review'}}))}>Report review</button></article>)}</section></>}</section>
}

export { default as Management } from './Management'
