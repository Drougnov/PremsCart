import Icon from './Icon'

export default function Help({path='/help'}:{path?:string}){
  if(path==='/about') return <section className="page-section help-page">
    <span className="eyebrow">ABOUT PREMSCART</span>
    <h1>A campus marketplace with a clear purpose</h1>
    <p className="help-intro">PremsCart helps Premier University students buy, rent, sell, give away, and request useful items without mixing those tasks across unrelated pages.</p>
    <div className="help-guides">
      <article><Icon name="market"/><h2>Find what you need</h2><p>Shop brings sale items, rentals, and giveaways into one searchable place.</p></article>
      <article><Icon name="message"/><h2>Talk to the other student</h2><p>Item conversations, offers, and order updates stay connected to the item or request they belong to.</p></article>
      <article><Icon name="package"/><h2>Meet on campus</h2><p>After a request is accepted, both students agree on a pickup at Main gate, Canteen, or Library.</p></article>
      <article><Icon name="shield"/><h2>Built around campus accounts</h2><p>Members verify access to an allowed university email before using marketplace features.</p></article>
    </div>
    <div className="help-cta"><h2>Explore PremsCart</h2><a className="button button-primary" href="/marketplace">Browse Shop <Icon name="arrow"/></a><a className="inline-link" href="/verification">How verification works</a></div>
  </section>

  if(path==='/safety') return <section className="page-section help-page">
    <span className="eyebrow">CAMPUS SAFETY</span>
    <h1>Safer handoffs, simpler rules</h1>
    <p className="help-intro">Keep the important parts of an exchange inside PremsCart and use the agreed campus pickup workflow before confirming a handoff.</p>
    <div className="help-guides">
      <article><Icon name="package"/><h2>Meet in the listed campus places</h2><p>Use Main gate, Canteen, or Library and choose a time when other people are around.</p></article>
      <article><Icon name="message"/><h2>Keep arrangements in Messages</h2><p>Use the item conversation for changes to pickup plans, rental returns, or questions about condition.</p></article>
      <article><Icon name="check"/><h2>Inspect before confirming</h2><p>Confirm pickup only after you have received and checked the item. Sellers should confirm rental returns only after getting the item back.</p></article>
      <article><Icon name="shield"/><h2>Protect your account</h2><p>Never share passwords or verification codes. Use Report on an item, profile, review, or message when something needs moderation.</p></article>
    </div>
    <div className="help-cta"><h2>Need help with a specific workflow?</h2><a className="button button-primary" href="/help">Open Help <Icon name="arrow"/></a><a className="inline-link" href="/dashboard/reviews">View my reports</a></div>
  </section>

  return <section className="page-section help-page"><span className="eyebrow">HELP & CAMPUS SAFETY</span><h1>How PremsCart works</h1><p className="help-intro">PremsCart connects Premier University students through buying, renting, and giving useful things a new home.</p><div className="help-guides">{[
 ['market','Buy something','Open an item, review the purchase or add it to your cart, then send your request. Once the seller accepts, agree on a pickup. Payment is arranged directly between you.'],
 ['clock','Rent something','Choose your preferred pickup/return dates and duration. The owner reserves the item after accepting. Your paid rental duration starts when you confirm the actual pickup; the displayed due time is authoritative. Ask the owner in chat if your plans change.'],
 ['gift','Give something away','Post a Giveaway at no charge. Choose a recipient from the requests, arrange pickup, and complete the handoff.'],
 ['shield','Meet with confidence','Use Main gate, Canteen, or Library. Choose a busy time, keep arrangements in chat, inspect the item, and confirm pickup only after you receive it. Never share passwords or verification codes.'],
 ].map(([icon,title,text])=><article key={title}><Icon name={icon as 'market'|'clock'|'gift'|'shield'}/><h2>{title}</h2><p>{text}</p></article>)}</div><section className="faq-section"><h2>A few common questions</h2>{[
 ['Does “verified” mean enrolled?','The badge confirms access to an allowed university email address. It does not independently check identity or current enrollment.'],
 ['When do I pay?','Arrange payment with the seller at pickup after inspecting the item. PremsCart does not collect money or process refunds.'],
 ['Is an item reserved in my cart?','No. The seller reserves it only when they accept your request. Prices and availability are checked again at checkout.'],
 ['Can I book a rental far in advance?','You can request a preferred pickup date within 90 days, for 1–30 days. An item can only have one active reservation at a time, so overlapping rental bookings are not available. Confirm date availability with the owner.'],
 ['How do I return a rental?','Open My orders, select “I’m ready to return it,” and agree on a campus handoff in chat. The owner confirms receiving it back, then both participants can review.'],
 ['What if something goes wrong?','Keep the conversation in chat, open the item, student review, or message and use Report. Moderators review reports and can hide content or restrict accounts. You can follow your report under Reviews & reports.'],
 ['What is saved on this browser?','Your theme, account-scoped cart, recent searches, recently viewed item IDs, and explicitly saved item-post text drafts use browser storage. Images are not saved in text drafts. Sign out on a shared device.'],
 ].map(([q,a])=><details key={q}><summary>{q}<Icon name="plus"/></summary><p>{a}</p></details>)}</section><div className="help-cta"><h2>Ready for your next good find?</h2><a className="button button-primary" href="/marketplace">Browse Shop<Icon name="arrow"/></a><a className="inline-link" href="/safety">Campus safety guidance</a></div></section>
}
