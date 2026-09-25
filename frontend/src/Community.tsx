import { toast } from './UI'
import { useLoad } from './Pages'
import { useEffect, useState, type FormEvent } from 'react'

type Review = { id: number; rating: number; comment: string | null; reviewerName: string; productTitle: string; createdAt: string }
type Reputation = { id: number; name: string; isVerified: boolean; reviewCount: number; averageRating: number | null; reviews: Review[] }
type Report = { id: number; reportedProductId: number | null; reportedUserId: number | null; productTitle?: string | null; userName?: string | null; reporterName?: string; reason: string; status: string; createdAt: string }
type ReviewTarget = { orderId: number; productTitle: string }
type ReportTarget = { productId?: number; userId?: number; reviewId?: number; messageId?: number; name: string }

async function api<T>(url: string, token: string, payload?: object): Promise<T> {
  const res = await fetch(url, { method: payload ? 'POST' : 'GET', headers: { Authorization: `Bearer ${token}`, ...(payload ? { 'Content-Type': 'application/json' } : {}) }, body: payload ? JSON.stringify(payload) : undefined })
  const value = await res.json().catch(() => ({}))
  if (!res.ok) throw new Error(value.error ?? value.title ?? `Request failed (${res.status})`)
  return value as T
}

export default function Community({ token }: { token: string }) {
  const [role, setRole] = useState('')
  const reviews=useLoad('/api/community/reviews/mine')
  const [reviewTarget, setReviewTarget] = useState<ReviewTarget | null>(null)
  const [reportTarget, setReportTarget] = useState<ReportTarget | null>(null)
  const [rating, setRating] = useState('5')
  const [comment, setComment] = useState('')
  const [reason, setReason] = useState('')
  const [reputation, setReputation] = useState<Reputation | null>(null)
  const [mine, setMine] = useState<Report[]>([])
  const [queue, setQueue] = useState<Report[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const isModerator = role === 'Moderator' || role === 'Admin'
  useEffect(() => {
    setRole(''); setMine([]); setQueue([]); setReviewTarget(null); setReportTarget(null); setReputation(null); setError('')
    if (!token) return
    let active = true
    Promise.all([api<{ role: string }>('/api/users/profile', token), api<Report[]>('/api/community/reports/mine', token)])
      .then(([profile, reports]) => { if (active) { setRole(profile.role); setMine(reports) } })
      .catch(e => { if (active) setError(e.message) })
    return () => { active = false }
  }, [token])
  useEffect(() => {
    if (!token || !isModerator) return
    api<Report[]>('/api/community/reports/moderation', token).then(setQueue).catch(e => setError(e.message))
  }, [token, isModerator])
  useEffect(() => {
    const review = (e: Event) => { setReviewTarget((e as CustomEvent<ReviewTarget>).detail); setError(''); document.getElementById('community')?.scrollIntoView({ behavior: 'smooth' }) }
    const report = (e: Event) => { setReportTarget((e as CustomEvent<ReportTarget>).detail); setError(''); document.getElementById('community')?.scrollIntoView({ behavior: 'smooth' }) }
    const profile = (e: Event) => {
      const id = (e as CustomEvent<number>).detail
      if (!token || !id) return
      setError(''); api<Reputation>(`/api/community/reputation/${id}`, token).then(setReputation).catch(err => setError(err.message))
      document.getElementById('community')?.scrollIntoView({ behavior: 'smooth' })
    }
    window.addEventListener('premscart-review', review); window.addEventListener('premscart-report', report); window.addEventListener('premscart-reputation', profile)
    return () => { window.removeEventListener('premscart-review', review); window.removeEventListener('premscart-report', report); window.removeEventListener('premscart-reputation', profile) }
  }, [token])
  async function sendReview(e: FormEvent) {
    e.preventDefault(); if (!reviewTarget) return
    setBusy(true); setError(''); setNotice('')
    try {
      await api('/api/community/reviews', token, { orderId: reviewTarget.orderId, rating: Number(rating), comment })
      setReviewTarget(null); setComment(''); setNotice('Review posted.');toast('Review posted.')
    } catch (err) { setError(err instanceof Error ? err.message : 'Review failed.') } finally { setBusy(false) }
  }
  async function sendReport(e: FormEvent) {
    e.preventDefault(); if (!reportTarget) return
    setBusy(true); setError(''); setNotice('')
    try {
      await api('/api/community/reports', token, { productId: reportTarget.productId ?? null, userId: reportTarget.userId ?? null, reviewId: reportTarget.reviewId ?? null, messageId: reportTarget.messageId ?? null, reason })
      setMine(await api<Report[]>('/api/community/reports/mine', token)); setReportTarget(null); setReason(''); setNotice('Report sent to moderation.');toast('Report sent to moderation.')
    } catch (err) { setError(err instanceof Error ? err.message : 'Report failed.') } finally { setBusy(false) }
  }
  return <section className="feature-section community-section" id="community"><div className="section-heading"><div><span className="eyebrow">CAMPUS TRUST</span><h2>Reviews & safety</h2><p>Share feedback after pickup and flag listings for review.</p></div></div>
    {!token ? <p className="feature-empty">Sign in to review or report.</p> : <>
      {error && <p role="alert" className="form-error">{error}</p>}{notice && <p role="status" className="form-success">{notice}</p>}
      {reviewTarget && <form className="community-form" onSubmit={sendReview}><h3>Review your order: {reviewTarget.productTitle}</h3><label>Rating<select value={rating} onChange={e => setRating(e.target.value)}>{[5, 4, 3, 2, 1].map(x => <option key={x} value={x}>{x} star{x !== 1 && 's'}</option>)}</select></label><label>Comment<textarea maxLength={1000} rows={3} value={comment} onChange={e => setComment(e.target.value)} /></label><div className="transaction-buttons"><button className="market-action" disabled={busy}>Post review</button><button type="button" onClick={() => setReviewTarget(null)}>Cancel</button></div></form>}
      {reportTarget && <form className="community-form" onSubmit={sendReport}><h3>Report {reportTarget.name}</h3><label>What happened?<textarea required minLength={5} maxLength={1000} rows={3} value={reason} onChange={e => setReason(e.target.value)} /></label><div className="transaction-buttons"><button className="market-action" disabled={busy}>Send report</button><button type="button" onClick={() => setReportTarget(null)}>Cancel</button></div></form>}
      {reputation && <div className="community-form"><div className="transaction-card-heading"><h3>{reputation.name} {reputation.isVerified && <span className="type-pill">University email verified</span>}</h3><button onClick={() => setReputation(null)}>Close</button></div><p>{reputation.reviewCount ? `${reputation.averageRating?.toFixed(1)} / 5 · ${reputation.reviewCount} reviews` : 'No reviews yet'}</p>{reputation.reviews.map(x => <article className="review-row" key={x.id}><strong>{'★'.repeat(x.rating)}{'☆'.repeat(5 - x.rating)}</strong> · {x.reviewerName} · {x.productTitle}<p>{x.comment || 'No comment'}</p></article>)}</div>}
      <div className="community-form"><h3>My reviews</h3>{reviews.error&&<p role="alert">{reviews.error}</p>}{reviews.data?.length===0&&<p>No reviews yet. Complete an order to leave feedback.</p>}{reviews.data?.map((r:any)=><p key={r.id}>Order #{r.orderId} · {r.rating}/5 · {r.comment} · <a href={`/students/${r.reviewedUserId}`}>Student profile</a></p>)}</div>
      <div className="community-form"><h3>My reports</h3>{mine.length ? mine.map(r => <p key={r.id}>#{r.id} · {r.status} · {r.reason}</p>) : <p>No reports submitted.</p>}</div>
      {isModerator && <div className="community-form"><h3>Moderation queue</h3>{queue.length ? queue.map(r => <article key={r.id} className="review-row"><strong>#{r.id} · {r.productTitle ?? r.userName ?? 'User'} </strong><p>From {r.reporterName}: {r.reason}</p><div className="transaction-buttons"><a className="button button-secondary" href="/moderator/reports">Review report</a></div></article>) : <p>No pending reports.</p>}</div>}
    </>}
  </section>
}
