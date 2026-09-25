import { go } from './api'
import { toast } from './UI'
import { useEffect, useState, type FormEvent } from 'react'

type Profile = {
  id: number
  firstName: string
  lastName: string
  universityEmail: string
  batch: number | null
  department: string | null
  isVerified: boolean
  role: string
}
type Mode = 'register' | 'verify' | 'login' | 'profile'

async function api<T>(path: string, body?: unknown, token?: string, method = 'POST'): Promise<T> {
  const response = await fetch(path, {
    method: body ? method : 'GET',
    headers: { ...(body ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body ? JSON.stringify(body) : undefined,
  })
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.error ?? data.title ?? 'The request could not be completed.')
  return data as T
}

export default function AuthPanel({path}: {path:string}) {
  const [mode, setMode] = useState<Mode>(path==='/register'?'register':path==='/verify-email'?'verify':'login')
  const [token, setToken] = useState(() => sessionStorage.getItem('premscart-token') ?? '')
  const [profile, setProfile] = useState<Profile | null>(null)
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState(sessionStorage.getItem('premscart-email')??'')
  const [password, setPassword] = useState('')
  const [code, setCode] = useState('')
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    if (!token) return
    api<Profile>('/api/users/profile', undefined, token)
      .then(user => { setProfile(user); setFirstName(user.firstName); setLastName(user.lastName); setMode('profile') })
      .catch(() => { sessionStorage.removeItem('premscart-token'); setToken(''); window.dispatchEvent(new Event('premscart-auth')) })
  }, [token])

  function switchMode(next: Mode) { go(next==='verify'?'/verify-email':next==='register'?'/register':next==='profile'?'/settings/profile':'/login') }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true); setError(''); setMessage('')
    try {
      if (mode === 'register') {
        const result = await api<{ message: string }>('/api/auth/register', { firstName, lastName, email, password })
        setMessage(result.message); sessionStorage.setItem('premscart-email',email); go('/verify-email')
      } else if (mode === 'verify') {
        const result = await api<{ message: string }>('/api/auth/verify-email', { email, code })
        setMessage(result.message); go('/login')
      } else if (mode === 'login') {
        const result = await api<{ token: string; profile: Profile }>('/api/auth/login', { email, password })
        sessionStorage.setItem('premscart-token', result.token); setToken(result.token)
        window.dispatchEvent(new Event('premscart-auth'))
        setProfile(result.profile); setFirstName(result.profile.firstName); setLastName(result.profile.lastName)
        setPassword(''); const next=new URLSearchParams(location.search).get('next'); const destination=next&&next.startsWith('/')&&!next.startsWith('//')&&!/^\/(login|register|verify-email)/.test(next)?next:'/dashboard'; go(result.profile.role==='Admin'?'/admin':result.profile.role==='Moderator'?'/moderator':destination)
      } else if (token) {
        const result = await api<Profile>('/api/users/profile', { firstName, lastName }, token, 'PUT')
        setProfile(result); setMessage('Profile updated.');toast('Profile updated.');window.dispatchEvent(new Event('premscart-profile-change'))
      }
    } catch (failure) { setError(failure instanceof Error ? failure.message : 'Something went wrong.') }
    finally { setBusy(false) }
  }

  async function resend() {
    setBusy(true); setError(''); setMessage('')
    try { const result = await api<{ message: string }>('/api/auth/resend-code', { email }); setMessage(result.message) }
    catch (failure) { setError(failure instanceof Error ? failure.message : 'Could not resend code.') }
    finally { setBusy(false) }
  }

  function signOut() {
    sessionStorage.removeItem('premscart-token'); setToken(''); setProfile(null)
    window.dispatchEvent(new Event('premscart-auth'))
    setPassword(''); setMessage(''); switchMode('login')
  }

  return <section className="auth-area" id="account">
    <div className="auth-copy"><p className="eyebrow">YOUR CAMPUS ACCOUNT</p><h2>{profile ? `Welcome, ${profile.firstName}.` : 'Join the campus community.'}</h2><p>Use your Premier University student email to create an account. Verify your email before signing in and using the marketplace.</p><div className="verify-note">Verification confirms access to your university email, not enrollment records.</div></div>
    <div className="auth-card">
      <div className="auth-card-header"><h3>{mode === 'register' ? 'Create account' : mode === 'verify' ? 'Verify your email' : mode === 'profile' ? 'Your profile' : 'Student sign in'}</h3>{profile && <button type="button" className="text-button" onClick={signOut}>Sign out</button>}</div>
      {profile && <div className="verified-pill">✓ University email verified · {profile.role}</div>}
      {message && <p className="form-message" role="status">{message}</p>}
      {error && <p className="form-error" role="alert">{error}</p>}
      <form onSubmit={submit}>
        {(mode === 'register' || mode === 'profile') && <div className="name-fields"><label>First name<input required maxLength={80} autoComplete="given-name" value={firstName} onChange={e => setFirstName(e.target.value)} /></label><label>Last name<input required maxLength={80} autoComplete="family-name" value={lastName} onChange={e => setLastName(e.target.value)} /></label></div>}
        {mode !== 'profile' && <label>University email<input required type="email" autoComplete="email" placeholder="name_44009@bscse.puc.ac.bd" value={email} onChange={e => setEmail(e.target.value)} /></label>}
        {(mode === 'register' || mode === 'login') && <label>Password<input required minLength={mode === 'register' ? 8 : undefined} type="password" autoComplete={mode === 'register' ? 'new-password' : 'current-password'} value={password} onChange={e => setPassword(e.target.value)} /></label>}
        {mode === 'verify' && <label>Six-digit verification code<input required inputMode="numeric" pattern="[0-9]{6}" maxLength={6} value={code} onChange={e => setCode(e.target.value)} /></label>}
        {mode === 'profile' && <p className="profile-detail">{profile?.universityEmail}<br />{profile?.department} · Batch {profile?.batch}</p>}
        <button className="primary-button" type="submit" disabled={busy}>{busy ? 'Please wait…' : mode === 'register' ? 'Register' : mode === 'verify' ? 'Verify email' : mode === 'profile' ? 'Save profile' : 'Sign in'}</button>
      </form>
      {mode === 'verify' && <button type="button" className="text-button" disabled={busy || !email} onClick={resend}>Send a new code</button>}
      {!profile && <div className="auth-switch">{mode === 'login' ? <>New student? <button onClick={() => switchMode('register')}>Create an account</button></> : <>Already registered? <button onClick={() => switchMode('login')}>Sign in</button></>}{mode !== 'verify' && mode !== 'login' && <> · <button onClick={() => switchMode('verify')}>Enter a code</button></>}</div>}
    </div>
  </section>
}
