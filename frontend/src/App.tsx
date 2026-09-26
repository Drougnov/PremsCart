import Loader from './Loader'
import ProfileSettings, { Avatar } from './ProfileSettings'
import { RequestProvider } from './RequestState'
import { useEffect, useRef, useState } from 'react'
import AuthPanel from './AuthPanel'
import Marketplace from './Marketplace'
import StudentFeatures from './StudentFeatures'
import ChatPanel from './ChatPanel'
import Transactions from './Transactions'
import Community from './Community'
import Stores from './Stores'
import Home from './Home'
import Cart from './Cart'
import Help from './Help'
import { FeedbackHost } from './UI'
import Preferences from './Preferences'
import StudentDashboard from './StudentDashboard'
import AdminWorkspace from './AdminWorkspace'
import { useCart } from './cart'
import Icon, { type IconName } from './Icon'
import { Notifications, Security, StudentProfile, Management } from './Pages'
import { api, go, signout } from './api'

type NavItem = [string, string, IconName]
type NavGroup = { label: string; items: NavItem[] }

const personalGroups: NavGroup[] = [
  { label: 'Overview', items: [['/dashboard','Dashboard','dashboard'],['/dashboard/listings','My items','listing']] },
  { label: 'Orders & deals', items: [['/dashboard/purchases','My orders','purchase'],['/dashboard/sales','Incoming requests','sale'],['/dashboard/offers','Price offers','offer']] },
  { label: 'Community', items: [['/dashboard/wishlist','Saved items','heart'],['/dashboard/wanted','My requests','wanted'],['/dashboard/reviews','Reviews & reports','star'],['/dashboard/notifications','Notifications','bell']] },
  { label: 'Store', items: [['/dashboard/store','Store profile','store'],['/dashboard/store/inventory','Store items','inventory']] },
  { label: 'Account', items: [['/settings/profile','Profile','user'],['/settings/security','Security','lock'],['/settings/notifications','Notification settings','bell']] },
]

const adminGroups: NavGroup[] = [
 {label:'Platform',items:[['/admin','Overview','dashboard'],['/admin/users','Users & roles','user']]},
 {label:'Marketplace',items:[['/admin/listings','Products & rentals','market'],['/admin/wanted','Requests','wanted'],['/admin/stores','Student shops','store'],['/admin/transactions','All transactions','purchase'],['/admin/rentals','Rental management','clock']]},
 {label:'Trust & safety',items:[['/moderator/reports','Reports','shield'],['/admin/reviews','Reviews','star']]},
 {label:'Configuration',items:[['/admin/categories','Categories','listing'],['/admin/departments','Departments','settings'],['/admin/pickup-locations','Pickup locations','package']]},
 {label:'Account',items:[['/admin/notifications','Notifications','bell'],['/settings/profile','My profile','user'],['/settings/security','Security','lock'],['/settings/notifications','Notification settings','bell']]},
]

const moderatorGroups: NavGroup[] = [
  { label: 'Moderation', items: [['/moderator','Overview','dashboard'],['/moderator/reports','Reports','shield'],['/moderator/users','Users','user']] },
  { label: 'Account', items: [['/settings/profile','My profile','user'],['/settings/security','Security','lock'],['/settings/notifications','Notification settings','bell']] },
]

const mainNav: NavItem[] = [
  ['/','Home','home'],['/marketplace','Shop','market'],['/wanted','Requests','wanted'],['/stores','Stores','store'],['/messages','Messages','message'],
]

export default function App() {
  const [routeKey,setRouteKey]=useState(location.pathname+location.search)
  const [path,setPath]=useState(location.pathname || '/')
  const [token,setToken]=useState(sessionStorage.getItem('premscart-token')??'')
  const [profileVersion,setProfileVersion]=useState(0)
  useEffect(()=>{const update=()=>setProfileVersion(v=>v+1);window.addEventListener('premscart-profile-change',update);return()=>window.removeEventListener('premscart-profile-change',update)},[])
  const [profile,setProfile]=useState<any>(null), [profileError,setProfileError]=useState(''), [unread,setUnread]=useState(0)
  const [dark,setDark]=useState(localStorage.getItem('theme')==='dark')
  const [menuOpen,setMenuOpen]=useState(false)
  const pending=useRef<{name:string;detail:any}|null>(null)

  useEffect(()=>{document.documentElement.classList.toggle('dark',dark);localStorage.setItem('theme',dark?'dark':'light')},[dark])
  useEffect(()=>{
    const pop=()=>{setPath(location.pathname || '/');setRouteKey(location.pathname+location.search);setMenuOpen(false)}
    const auth=()=>{setProfile(null);setToken(sessionStorage.getItem('premscart-token')??'');setProfileVersion(v=>v+1)}
    window.addEventListener('popstate',pop);window.addEventListener('premscart-auth',auth)
    const click=(e:MouseEvent)=>{const a=(e.target as HTMLElement).closest('a');if(a&&a.origin===location.origin&&a.pathname.startsWith('/')&&!a.getAttribute('href')?.startsWith('#')&&!a.download&&!a.target&&!e.ctrlKey&&!e.metaKey&&!e.shiftKey&&!e.altKey&&e.button===0){e.preventDefault();go(a.pathname+a.search)}}
    document.addEventListener('click',click)
    return()=>{window.removeEventListener('popstate',pop);window.removeEventListener('premscart-auth',auth);document.removeEventListener('click',click)}
  },[])
  useEffect(()=>{setProfileError('');if(!token){setProfile(null);return;}let active=true;api('/api/users/profile').then(p=>{if(active)setProfile(p)}).catch(e=>{if(active)setProfileError(e.message)});return()=>{active=false}},[token,profileVersion])
  useEffect(()=>{if(!token)return;let active=true;const poll=()=>api('/api/notifications').then(n=>{if(active)setUnread(n.unread)}).catch(()=>{});void poll();window.addEventListener('premscart-notifications-change',poll);const timer=setInterval(poll,30000);return()=>{active=false;clearInterval(timer);window.removeEventListener('premscart-notifications-change',poll)}},[token,path])
  useEffect(()=>{const names=['premscart-open-product','premscart-open-chat','premscart-start-transaction','premscart-reputation','premscart-open-store','premscart-report','premscart-review'];const handler=(e:Event)=>{const detail=(e as CustomEvent).detail;const destination=e.type==='premscart-open-product'?`/listings/${detail}`:e.type==='premscart-open-chat'?`/messages/${detail}`:e.type==='premscart-open-store'?`/stores/${detail}`:e.type==='premscart-reputation'?`/students/${detail}`:e.type==='premscart-start-transaction'?'/dashboard/offers':'/dashboard/reviews';if(path!==destination){pending.current={name:e.type,detail};go(destination)}};names.forEach(n=>window.addEventListener(n,handler));return()=>names.forEach(n=>window.removeEventListener(n,handler))},[path])
  useEffect(()=>{if(pending.current){const event=pending.current;pending.current=null;window.dispatchEvent(new CustomEvent(event.name,{detail:event.detail}))}},[path])

  const isAdmin=profile?.role==='Admin'
  const isModerator=profile?.role==='Moderator'
  const cart=useCart(profile?.id)
  useEffect(()=>{if(isAdmin&&(path==='/'||path.startsWith('/dashboard')||path==='/cart'||path==='/listings/new'))go(path==='/dashboard/notifications'?'/admin/notifications':'/admin')},[isAdmin,path])
  useEffect(()=>{if(menuOpen){const close=(e:KeyboardEvent)=>{if(e.key==='Escape')setMenuOpen(false)};window.addEventListener('keydown',close);return()=>window.removeEventListener('keydown',close)}},[menuOpen])
  useEffect(()=>{const t=setTimeout(()=>{const main=document.querySelector<HTMLElement>('main');const heading=main?.querySelector('h1,h2')?.textContent;document.title=heading?`${heading} | PremsCart`:'PremsCart · Your campus marketplace';main?.focus({preventScroll:true})},120);return()=>clearTimeout(t)},[routeKey])
  const authPage=['/login','/register','/verify-email','/forgot-password'].includes(path)
  const staff=profile&&['Moderator','Admin'].includes(profile.role)
  const protectedStaff=path.startsWith('/admin')?profile?.role==='Admin':path.startsWith('/moderator')?staff:true
  const sidebar=isAdmin||path.startsWith('/dashboard')||path.startsWith('/settings')||path.startsWith('/admin')||path.startsWith('/moderator')
  const sidebarGroups=isAdmin?adminGroups:isModerator?moderatorGroups:personalGroups

  let page
  if(path==='/') page=<Home token={token} firstName={profile?.firstName} admin={isAdmin}/>
  else if(path==='/help'||path==='/about'||path==='/safety') page=<Help path={path}/>
  else if(path==='/verification') page=<section className="feature-section verification-page"><span className="eyebrow">KNOW YOUR COMMUNITY</span><h1>What does verified mean?</h1><p>Students register with an allowed Premier University student email address. We send a six-digit code to that mailbox. A correct, unexpired code marks the email as verified and enables marketplace access.</p><ol><li>Use your permitted university email.</li><li>Enter the code within 10 minutes. Five incorrect attempts lock that code.</li><li>Sign in to buy, rent, sell, give items away, post requests, and chat.</li></ol><p>The badge confirms access to that email address. It is not a separate identity or enrollment check.</p><a className="button button-primary" href={token?'/marketplace':'/register'}>{token?'Browse Shop':'Create an account'} <Icon name="arrow"/></a></section>
  else if(!token&&!authPage) page=<section className="feature-section sign-in-gate"><span className="gate-icon"><Icon name="lock"/></span><span className="eyebrow">VERIFIED STUDENTS ONLY</span><h1>Sign in to continue</h1><p>PremsCart is a campus marketplace for buying, renting, selling, giving away, and requesting items. Sign in with a verified Premier University account to continue.</p><div className="quick-actions"><a className="button button-primary" href={`/login?next=${encodeURIComponent(path+location.search)}`}>Sign in</a><a className="button button-secondary" href="/register">Create account</a></div><a className="inline-link" href="/">Back to homepage</a></section>
  else if(profileError&&!authPage) page=<section className="feature-section status-panel"><span className="gate-icon"><Icon name="shield"/></span><h1>We could not load your account</h1><p role="alert">{profileError}</p><button className="button button-primary" onClick={signout}>Sign in again</button></section>
  else if(token&&!profile&&!authPage) page=<Loader key={profileVersion} label="Loading your account…" onRetry={()=>setProfileVersion(v=>v+1)}/>
  else if(!protectedStaff) page=<section className="feature-section status-panel"><span className="gate-icon"><Icon name="shield"/></span><h1>Access restricted</h1><p>This page requires a campus management role.</p><a className="button button-secondary" href="/dashboard">Return to dashboard</a></section>
  else if(path==='/forgot-password') page=<Security recovery/>
  else if(authPage) page=<><AuthPanel path={path}/>{token&&<a className="button button-primary continue-dashboard" href="/dashboard">Continue to dashboard <Icon name="arrow"/></a>}</>
  else if(path==='/settings/notifications') page=<Preferences/>
  else if(path==='/settings/security') page=<Security/>
  else if(path==='/settings/profile') page=<ProfileSettings token={token}/>
  else if(path==='/admin/notifications') page=<Notifications/>
  else if(path==='/cart') page=<Cart userId={profile.id} token={token}/>
  else if(['/admin','/admin/listings','/admin/wanted','/admin/stores','/admin/reviews','/admin/transactions','/admin/rentals'].includes(path)) page=<AdminWorkspace key={path} path={path}/>
  else if(path.startsWith('/admin')||path.startsWith('/moderator')) page=<Management key={path} path={path}/>
  else if(path==='/dashboard') page=<StudentDashboard/>
  else if(path==='/dashboard/notifications') page=<Notifications/>
  else if(path.startsWith('/students/')) page=<StudentProfile id={Number(path.split('/')[2])} token={token}/>
  else if(path.startsWith('/messages')) page=<ChatPanel token={token} path={path}/>
  else if(path.startsWith('/orders/')) page=<Transactions token={token} path={path}/>
  else if(['/dashboard/purchases','/dashboard/sales','/dashboard/offers'].includes(path)) page=<Transactions token={token} path={path}/>
  else if(path==='/dashboard/reviews') page=<Community token={token}/>
  else if(path.startsWith('/stores')||path.startsWith('/dashboard/store')) page=<Stores token={token} path={path}/>
  else if(path.startsWith('/wanted')||['/dashboard/wishlist','/dashboard/wanted'].includes(path)) page=<StudentFeatures token={token} path={path}/>
  else if(path==='/marketplace'||path==='/rentals'||path==='/giveaways'||path.startsWith('/listings/')||path==='/dashboard/listings') page=<Marketplace token={token} path={path}/>
  else page=<section className="feature-section status-panel"><h1>Page not found</h1><p>The page you requested does not exist.</p><a className="button button-primary" href="/">Go home</a></section>

  return <RequestProvider token={token} userId={profile?.id}><div className="app-shell"><a href="#main-content" className="skip-link" onClick={e=>{e.preventDefault();document.getElementById('main-content')?.focus()}}>Skip to content</a><FeedbackHost/>
    <header className="topbar">
      <div className="topbar-inner">
        <a className="brand" href="/" aria-label="PremsCart home"><img src="/brand/premscart-logo.jpeg" alt=""/><span><strong>PremsCart</strong><small>Premier University</small></span></a>
        <button className="mobile-menu-button icon-button" aria-label={menuOpen?'Close navigation':'Open navigation'} aria-expanded={menuOpen} onClick={()=>setMenuOpen(v=>!v)}><Icon name={menuOpen?'close':'menu'}/></button>
        <nav className={`main-nav ${menuOpen?'open':''}`} aria-label="Main navigation">{(isAdmin?([['/admin','Workspace','dashboard'],['/marketplace','View marketplace','market']] as NavItem[]):mainNav).filter(([url])=>token||url!=='/messages').map(([url,label,icon])=><a key={url} aria-current={path===url||(url!=='/'&&path.startsWith(url))?'page':undefined} href={url}><Icon name={icon}/><span>{label}</span></a>)}</nav>
        <div className="topbar-tools">
          {token&&!isAdmin&&!isModerator&&<a className="header-post button button-primary compact" href="/listings/new"><Icon name="plus"/> Post item</a>}
          {token&&!isAdmin&&!isModerator&&<a className="icon-button wishlist-nav" href="/dashboard/wishlist" aria-label="Wishlist" title="Wishlist"><Icon name="heart"/></a>}
          {!isModerator&&<a className="icon-button cart-nav" href={isAdmin?'/admin/transactions':'/cart'} aria-label={isAdmin?'All transactions':`Cart, ${cart.length} items`} title={isAdmin?'Transactions':'Your cart'}><Icon name="purchase"/>{!isAdmin&&cart.length>0&&<b className="badge">{cart.length}</b>}</a>}
          <button className="icon-button" onClick={()=>setDark(!dark)} aria-label={dark?'Use light theme':'Use dark theme'} title={dark?'Use light theme':'Use dark theme'}><Icon name={dark?'sun':'moon'}/></button>
          {token?<>
            <a className="icon-button notification-button" href={isAdmin?'/admin/notifications':'/dashboard/notifications'} aria-label={`${unread} unread notifications`} title="Notifications"><Icon name="bell"/>{unread>0&&<b className="badge">{unread>9?'9+':unread}</b>}</a>
            <details className="profile-menu"><summary><Avatar url={profile?.profileImage} token={token} name={profile?.firstName??'Account'}/><span className="profile-summary-text"><strong>{profile?.firstName??'Account'}</strong><small>{profile?.role??'Member'}</small></span><Icon name="chevron"/></summary><div className="profile-popover"><div className="profile-popover-head"><strong>{profile?`${profile.firstName} ${profile.lastName}`:'Your account'}</strong><span>{profile?.universityEmail}</span></div><a href={isAdmin?'/admin':isModerator?'/moderator':'/dashboard'}><Icon name="dashboard"/> {isAdmin?'Admin workspace':isModerator?'Moderation':'Dashboard'}</a><a href="/settings/profile"><Icon name="user"/> Profile settings</a>{staff&&<a href="/moderator"><Icon name="shield"/> Moderation</a>}{profile?.role==='Admin'&&<a href="/admin"><Icon name="settings"/> Administration</a>}<button onClick={signout}><Icon name="lock"/> Sign out</button></div></details>
          </>:<div className="guest-actions"><a className="button button-ghost" href="/login">Sign in</a><a className="button button-primary compact" href="/register">Join</a></div>}
        </div>
      </div>
    </header>

    <div className={sidebar&&token?'workspace-layout':''}>
      {sidebar&&token&&<aside className="dashboard-nav" aria-label="Dashboard navigation"><a className="sidebar-brand" href={isAdmin?'/admin':isModerator?'/moderator':'/dashboard'}><span className="sidebar-logo"><Icon name={isAdmin?'settings':isModerator?'shield':'dashboard'}/></span><span><strong>{isAdmin?'Administration':isModerator?'Moderation':'My PremsCart'}</strong><small>{isAdmin?'Manage the platform':isModerator?'Keep campus safe':'Everything has its own place'}</small></span></a>{sidebarGroups.map(group=><div className="sidebar-group" key={group.label}><span className="sidebar-label">{group.label}</span>{group.items.filter(([url])=>url!=='/dashboard/store/inventory'||profile?.role==='Business Seller').map(([url,label,icon])=><a key={url} href={url} aria-current={path===url?'page':undefined}><Icon name={icon}/><span>{label}</span>{url==='/dashboard/notifications'&&unread>0&&<b className="nav-count">{unread}</b>}</a>)}</div>)}</aside>}
      <main id="main-content" tabIndex={-1} key={routeKey} className={path==='/'?'home-main':''}>{page}</main>
    </div>

    {!isAdmin&&!isModerator&&<nav className="mobile-bottom-nav" aria-label="Quick navigation">{([['/','Home','home'],['/marketplace','Shop','search'],['/listings/new','Post','plus'],['/messages','Messages','message'],[token?'/dashboard':'/login','Account','user']] as NavItem[]).map(([href,label,icon])=><a key={href} href={href} aria-current={path===href?'page':undefined}><Icon name={icon}/><span>{label}</span></a>)}</nav>}
    <footer className="site-footer editorial-footer"><div className="footer-top"><div className="footer-story"><a className="footer-brand" href="/"><img src="/brand/premscart-logo.jpeg" alt=""/><span><strong>PremsCart</strong><small>Good things, closer to you.</small></span></a><p>Buy, rent, sell, or give things away with other verified Premier University students.</p><span className="footer-campus"><span/> Made for Premier University</span></div><nav aria-label="Explore"><h3>Explore</h3><a href="/marketplace">Shop campus items</a><a href="/rentals">Browse rentals</a><a href="/giveaways">Browse giveaways</a><a href="/stores">Student stores</a></nav><nav aria-label="Community"><h3>Community</h3><a href="/wanted">Requests</a><a href={isAdmin?'/admin/listings':'/listings/new'}>{isAdmin?'Manage products':'Post an item'}</a><a href="/messages">Messages</a><a href="/verification">How verification works</a><a href="/help">Help & campus safety</a><a href="/about">About PremsCart</a></nav><nav aria-label="Your space"><h3>Your space</h3><a href={isAdmin?'/admin':token?'/dashboard':'/login'}>{isAdmin?'Admin workspace':token?'Your dashboard':'Sign in'}</a><a href={isAdmin?'/admin/transactions':'/cart'}>{isAdmin?'All transactions':'Your cart'}</a><a href={token?'/settings/profile':'/register'}>{token?'Profile & settings':'Join PremsCart'}</a></nav></div><div className="footer-bottom"><span>© {new Date().getFullYear()} PremsCart. A campus community project.</span><span>Main gate · Canteen · Library <Icon name="package"/></span></div></footer>
  </div></RequestProvider>
}
