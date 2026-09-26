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
  notifications:'bell', wishlist:'heart', rating:'star', completed:'package', users:'user', listings:'market', transactions:'purchase', stores:'store', pendingReports:'shield', itemsSold:'sale', totalSales:'purchase', visibleItems:'store'
}
const metricLabels:Record<string,string>={activeListings:'Active items',pendingPurchases:'Pending purchases',unreadMessages:'Unread messages',givenAway:'Given away',itemsSold:'Items sold',totalSales:'Total sales',visibleItems:'Items visible in store'}
export function Metrics({data}: {data: any}) {
  const value=(key:string,raw:unknown)=>key==='totalSales'?`৳${Number(raw).toLocaleString(undefined,{maximumFractionDigits:0})}`:Number(raw).toLocaleString(undefined,{maximumFractionDigits:1})
  return <div className="metrics">{Object.entries(data).filter(([,v])=>typeof v==='number').map(([k,v])=><article className="metric-card" key={k}><span className="metric-icon"><Icon name={metricIcons[k]??'dashboard'}/></span><div><small>{metricLabels[k]??k.replace(/([A-Z])/g,' $1')}</small><strong>{value(k,v)}</strong></div></article>)}</div>
}

export function Notifications() {
  const {data,error,refresh}=useLoad('/api/notifications')
  const [issue,setIssue]=useState(''),[unreadOnly,setUnreadOnly]=useState(false),[busy,setBusy]=useState(false)
  async function read(id?:number){setBusy(true);setIssue('');try{await api(id?`/api/notifications/${id}/read`:'/api/notifications/read-all','POST',{});refresh();window.dispatchEvent(new Event('premscart-notifications-change'));return true}catch(e){setIssue((e as Error).message);return false}finally{setBusy(false)}}
  const rows=data?.items.filter((n:any)=>!unreadOnly||!n.isRead)??[]
  const typeInfo=(raw:string|undefined,title:string):{label:string;icon:IconName;key:string}=>{
    const type=(raw||'activity').toLowerCase()
    if(type==='messages'||title.toLowerCase().includes('message'))return {label:'Message',icon:'message',key:'message'}
    if(type==='offers'||title.toLowerCase().includes('offer'))return {label:'Price offer',icon:'offer',key:'offer'}
    if(type==='rentals')return {label:'Rental',icon:'clock',key:'rental'}
    if(type==='orders'||title.toLowerCase().includes('order'))return {label:'Order',icon:'purchase',key:'order'}
    if(type==='saved')return {label:'Saved item',icon:'heart',key:'saved'}
    if(type==='reviews')return {label:'Review',icon:'star',key:'review'}
    if(type==='moderation'||type==='reports')return {label:'Safety update',icon:'shield',key:'safety'}
    if(type==='account')return {label:'Account',icon:'user',key:'account'}
    return {label:'Update',icon:'bell',key:'activity'}
  }
  return <section className="page-section notifications-page">
    <div className="page-heading"><div><span className="eyebrow">UPDATES THAT NEED YOU</span><h1>Notifications</h1><p>Each update is labelled so you can quickly tell whether it is a message, order, offer, rental, or account update.</p></div><a className="button button-secondary" href="/settings/notifications">Choose notification types</a></div>
    {(error||issue)&&<ErrorState message={error||issue} retry={refresh}/>} 
    <div className="management-toolbar"><div className="catalog-tabs"><button className={!unreadOnly?'active':''} onClick={()=>setUnreadOnly(false)}>All</button><button className={unreadOnly?'active':''} onClick={()=>setUnreadOnly(true)}>Unread <span className="count-badge">{data?.unread??0}</span></button></div>{data?.unread>0&&<button className="button button-secondary compact" disabled={busy} onClick={async()=>{if(await read())toast('All notifications marked as read.')}}>Mark all as read</button>}</div>
    {!data&&!error?<Skeleton count={2} label="Loading notifications"/>:data&&<div className="notification-list">{!rows.length&&<EmptyState title="No unread updates." message={unreadOnly?'You have read everything. Switch to All to see older updates.':'New messages, orders, offers, and account updates will appear here.'} href="/marketplace" label="Browse items" icon="bell"/>}{rows.map((n:any)=>{const info=typeInfo(n.type,n.title);return <article className={`notification-row notification-${info.key} ${n.isRead?'read':'unread'}`} key={n.id}><span className="row-icon"><Icon name={info.icon}/></span><div className="notification-copy"><div className="notification-meta"><span className={`notification-type ${info.key}`}>{info.label}</span><span className={`read-state ${n.isRead?'read':'unread'}`}>{n.isRead?'Read':'Unread'}</span></div><h3>{n.title}</h3><p>{n.message}</p><small>{new Date(n.createdAt).toLocaleString()}</small></div><button className="button button-secondary compact" disabled={busy} onClick={async()=>{if(await read(n.id))go(n.link)}}>{n.isRead?'Open':'Read & open'} <Icon name="arrow"/></button></article>})}</div>}
  </section>
}

export function Security({recovery=false}:{recovery?:boolean}) {
  const [message,setMessage]=useState(''), [error,setError]=useState(''),[busy,setBusy]=useState(false)
  const [recoveryMode,setRecoveryMode]=useState<'request'|'reset'>('request')
  const [recoveryEmail,setRecoveryEmail]=useState('')
  async function submit(e:FormEvent<HTMLFormElement>){
    e.preventDefault();setBusy(true);setError('');setMessage('')
    const f=new FormData(e.currentTarget), action=String(f.get('action')||'change-password')
    try{
      const r=await api(`/api/auth/${action}`,'POST',Object.fromEntries(f));setMessage(r.message)
      if(action==='forgot-password'){setRecoveryEmail(String(f.get('email')||''));setRecoveryMode('reset')}
      if(action==='change-password')signout()
    }catch(e){setError((e as Error).message)}finally{setBusy(false)}
  }
  if(recovery)return <section className="feature-section narrow security-card">
    <span className="gate-icon"><Icon name="lock"/></span><span className="eyebrow">ACCOUNT RECOVERY</span><h1>Recover your account</h1>
    <p>{recoveryMode==='request'?'Enter your university email and we’ll send you a reset code.':'Enter the six-digit code you received and choose a new password.'}</p>
    {error&&<p className="form-error" role="alert">{error}</p>}{message&&<p className="form-success" role="status">{message}</p>}
    <div className="catalog-tabs" aria-label="Password recovery step"><button type="button" className={recoveryMode==='request'?'active':''} onClick={()=>{setRecoveryMode('request');setError('');setMessage('')}}>Get reset code</button><button type="button" className={recoveryMode==='reset'?'active':''} onClick={()=>{setRecoveryMode('reset');setError('');setMessage('')}}>I have a code</button></div>
    {recoveryMode==='request'?<form onSubmit={submit}><input type="hidden" name="action" value="forgot-password"/><label>University email<input name="email" type="email" required defaultValue={recoveryEmail}/></label><button className="button button-primary" disabled={busy}>{busy?'Sending…':'Send reset code'}</button></form>:<form onSubmit={submit}><input type="hidden" name="action" value="reset-password"/><label>University email<input name="email" type="email" required defaultValue={recoveryEmail}/></label><label>Reset code<input name="code" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} required/></label><label>New password<input name="password" type="password" autoComplete="new-password" minLength={8} maxLength={128} required/></label><button className="button button-primary" disabled={busy}>{busy?'Resetting…':'Reset password'}</button></form>}
    <a className="text-button" href="/login">Back to sign in</a>
  </section>
  return <section className="feature-section narrow security-card"><span className="gate-icon"><Icon name="lock"/></span><span className="eyebrow">ACCOUNT SECURITY</span><h1>Password & security</h1><p>Choose a strong password and keep your university account protected.</p>{error&&<p className="form-error" role="alert">{error}</p>}{message&&<p className="form-success" role="status">{message}</p>}<form onSubmit={submit}><input type="hidden" name="action" value="change-password"/><label>Current password<input name="currentPassword" type="password" autoComplete="current-password" required/></label><label>New password<input name="password" type="password" autoComplete="new-password" minLength={8} maxLength={128} required/></label><button className="button button-primary" disabled={busy}>{busy?'Changing…':'Change password'}</button></form></section>
}

export function PhotoUpload({store=false,onSaved}:{store?:boolean;onSaved?:()=>void}) {
  const [message,setMessage]=useState(''),[busy,setBusy]=useState(false)
  return <form className="photo-upload" onSubmit={async e=>{e.preventDefault();setBusy(true);try{await api(store?'/api/stores/mine/logo':'/api/users/profile/image','POST',new FormData(e.currentTarget));setMessage('Image saved.');onSaved?.()}catch(e){setMessage((e as Error).message)}finally{setBusy(false)}}}><div><span className="eyebrow">IMAGE</span><h3>{store?'Store logo':'Profile picture'}</h3></div><label>Select image<input type="file" name="file" accept="image/jpeg,image/png,image/webp" required/></label><button className="button button-primary" disabled={busy}>Upload image</button>{message&&<p role="status">{message}</p>}</form>
}

export function StudentProfile({id,token}:{id:number;token:string}) {
  const {data,error}=useLoad(`/api/users/${id}`), rep=useLoad(`/api/community/reputation/${id}`)
  return <section className="page-section profile-page"><State data={data} error={error}/>{data&&<><div className="profile-heading"><div className="profile-photo"><PrivateImage url={data.profile.profileImage} token={token} alt="Profile picture"/></div><div><span className="verified-pill"><Icon name="shield"/> University email verified</span><h1>{data.profile.firstName} {data.profile.lastName}</h1><p>{data.profile.department} · Batch {data.profile.batch}</p></div></div><Metrics data={{completed:data.completed,sold:data.sold,givenAway:data.givenAway,rating:rep.data?.averageRating??0}}/><section className="content-panel"><div className="panel-heading"><h2>Items available now</h2></div>{data.listings.length?<div className="compact-list">{data.listings.map((p:any)=><article className="simple-row" key={p.id}><a href={`/listings/${p.id}`}>{p.title}</a><strong>{p.transactionType==='Giveaway'?'Giveaway':`৳${p.price}`}</strong></article>)}</div>:<p className="feature-empty">No items available right now.</p>}</section><section className="content-panel"><div className="panel-heading"><h2>Student reviews</h2></div><State data={rep.data} error={rep.error}/>{rep.data?.reviews.map((r:any)=><article className="review-row" key={r.id}><div><h3><span className="rating-stars">★</span> {r.rating} / 5 · {r.reviewerName}</h3><p>{r.comment}</p></div><button className="text-button danger-text" onClick={()=>window.dispatchEvent(new CustomEvent('premscart-report',{detail:{reviewId:r.id,name:'Review'}}))}>Report review</button></article>)}</section></>}</section>
}

export { default as Management } from './Management'
