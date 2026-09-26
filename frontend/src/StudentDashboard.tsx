import { useLoad } from './Pages'
import { ErrorState, Skeleton, EmptyState } from './UI'
import Icon, { type IconName } from './Icon'

export default function StudentDashboard() {
  const { data,error,refresh } = useLoad('/api/dashboard/student')
  if (error) return <ErrorState message={error} retry={refresh}/>

  return <section className="page-section dashboard-page">
    <div className="page-heading">
      <div><span className="eyebrow">YOUR PREMSCART</span><h1>What needs your attention?</h1><p>This page is only a quick overview. Use the menu for your items, orders, messages, saved items, and store.</p></div>
    </div>

    {!data ? <Skeleton count={3} label="Loading your dashboard"/> : <>
      <div className="student-metrics">{([
        ['/dashboard/listings','Items available',data.activeListings,'listing'],
        ['/dashboard/purchases','Active rentals',data.activeRentals??0,'clock'],
        ['/dashboard/wishlist','Saved items',data.wishlist,'heart'],
        ['/dashboard/sales?history=true','Completed handoffs',(data.sold??0)+(data.givenAway??0),'package'],
      ] as [string,string,number,IconName][]).map(([href,label,count,icon]) => <a key={label} href={href}><Icon name={icon}/><div><strong>{count}</strong><span>{label}</span></div><Icon name="arrow"/></a>)}</div>

      <div className="dashboard-content-grid">
        <section className="content-panel next-actions">
          <div className="panel-heading"><div><span className="eyebrow">DO THESE NEXT</span><h2>Your next steps</h2></div></div>
          {(data.pendingSales||data.offersToRespond||data.unreadMessages||data.pendingPurchases) ? <>
            {data.pendingSales>0 && <Action href="/dashboard/sales" icon="sale" title={`${data.pendingSales} order request${data.pendingSales>1?'s':''} to review`} text="Someone wants one of your items."/>}
            {data.offersToRespond>0 && <Action href="/dashboard/offers" icon="offer" title={`${data.offersToRespond} price offer${data.offersToRespond>1?'s':''} waiting`} text="Accept it, decline it, or send a counter price."/>}
            {data.unreadMessages>0 && <Action href="/messages" icon="message" title={`${data.unreadMessages} unread message${data.unreadMessages>1?'s':''}`} text="Open Messages to continue the conversation."/>}
            {data.pendingPurchases>0 && <Action href="/dashboard/purchases" icon="purchase" title={`${data.pendingPurchases} request${data.pendingPurchases>1?'s':''} you sent`} text="See whether the seller accepted and arrange pickup."/>}
          </> : <EmptyState title="Nothing needs your attention." message="You are caught up. Browse campus items whenever you are ready." href="/marketplace" label="Browse Shop" icon="check"/>}
        </section>

        <section className="content-panel">
          <div className="panel-heading"><div><span className="eyebrow">COMING UP</span><h2>Campus pickups</h2></div><a className="inline-link" href="/dashboard/purchases">Open my orders<Icon name="arrow"/></a></div>
          {data.pickups?.length ? data.pickups.map((p:any) => <a className="upcoming-pickup" key={p.id} href={`/orders/${p.id}`}><span className="pickup-date"><strong>{new Date(p.pickupTime).getDate()}</strong><small>{new Date(p.pickupTime).toLocaleString(undefined,{month:'short'})}</small></span><div><h3>{p.title}</h3><p>{new Date(p.pickupTime).toLocaleString()}</p><span className="status-badge">{p.pickupStatus}</span></div><Icon name="arrow"/></a>) : <p className="helper-copy">When you and another student agree on a pickup time, it will appear here.</p>}
          <a className="inline-link" href="/help">How to meet safely on campus<Icon name="arrow"/></a>
        </section>
      </div>
    </>}
  </section>
}

function Action({href,icon,title,text}:{href:string;icon:IconName;title:string;text:string}) {
  return <a className="next-action" href={href}><span><Icon name={icon}/></span><div><strong>{title}</strong><small>{text}</small></div><Icon name="arrow"/></a>
}
