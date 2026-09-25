import { go } from './api'
import Icon from './Icon'
import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { HubConnectionBuilder, HubConnectionState, type HubConnection } from '@microsoft/signalr'

type Conversation = {
  id: number; productId: number; productTitle: string; otherUserId: number
  otherUserName: string; lastMessage: string | null; lastMessageAt: string
  unreadCount: number
}
type ChatMessage = {
  id: number; conversationId: number; senderId: number; messageText: string
  isRead: boolean; sentAt: string
}
type History = { items: ChatMessage[]; hasMore: boolean }

async function fetchJson<T>(url: string, token: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(url, { ...options, headers: { Authorization: `Bearer ${token}`, ...options.headers } })
  if (response.status === 204) return undefined as T
  const result = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(result.error ?? result.title ?? 'Could not load messages.')
  return result as T
}

export default function ChatPanel({ token, path }: { token: string; path: string }) {
  const [query,setQuery]=useState(''),[onlyUnread,setOnlyUnread]=useState(false)
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [hasMore, setHasMore] = useState(false)
  const [myId, setMyId] = useState<number | null>(null)
  const [otherOnline, setOtherOnline] = useState<boolean | null>(null)
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'reconnecting' | 'offline'>('offline')
  const [joined, setJoined] = useState(false)
  const [draft, setDraft] = useState('')
  const [sending, setSending] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const connectionRef = useRef<HubConnection | null>(null)
  const selectedRef = useRef<number | null>(null)
  const otherUserRef = useRef<number | null>(null)
  const endRef = useRef<HTMLDivElement | null>(null)

  const refreshInbox = useCallback(async () => {
    if (!token) return
    try { setConversations(await fetchJson<Conversation[]>('/api/chat/conversations', token)) }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not load conversations.') }
  }, [token])

  useEffect(() => {
    selectedRef.current = selectedId
    otherUserRef.current = conversations.find(c => c.id === selectedId)?.otherUserId ?? null
  }, [selectedId, conversations])

  useEffect(() => {
    setConversations([]); setSelectedId(Number(path.split('/')[2])||null); setMessages([]); setOtherOnline(null); setJoined(false)
    if (!token) { setConnectionStatus('offline'); return }
    let disposed = false
    setConnectionStatus('connecting'); setError('')
    void refreshInbox()
    void fetchJson<{ id: number }>('/api/users/profile', token).then(user => { if (!disposed) setMyId(user.id) })
      .catch(() => { if (!disposed) setMyId(null) })
    const connection = new HubConnectionBuilder()
      .withUrl('/chatHub', { accessTokenFactory: () => token })
      .withAutomaticReconnect().build()
    connectionRef.current = connection
    connection.on('ReceiveMessage', (message: ChatMessage) => {
      if (selectedRef.current === message.conversationId) {
        setMessages(items => items.some(x => x.id === message.id) ? items : [...items, message])
        void fetchJson<void>(`/api/chat/conversations/${message.conversationId}/read`, token, { method: 'POST' }).catch(() => {})
      }
      void refreshInbox()
    })
    connection.on('InboxChanged', () => { void refreshInbox() })
    connection.on('PresenceChanged', (presence: { userId: number; online: boolean }) => {
      if (otherUserRef.current === presence.userId) setOtherOnline(presence.online)
    })
    connection.onreconnecting(() => { if (!disposed) { setConnectionStatus('reconnecting'); setJoined(false) } })
    connection.onreconnected(() => { if (!disposed) setConnectionStatus('connected') })
    connection.onclose(() => { if (!disposed) { setConnectionStatus('offline'); setJoined(false) } })
    void connection.start().then(() => { if (!disposed) setConnectionStatus('connected') })
      .catch(() => { if (!disposed) { setConnectionStatus('offline'); setError('Live chat is unavailable. Check the API and retry.') } })
    return () => {
      disposed = true; connectionRef.current = null
      void connection.stop()
    }
  }, [token, retry, refreshInbox])

  useEffect(() => {
    if (!token || selectedId === null) return
    let cancelled = false
    setLoading(true); setMessages([]); setHasMore(false); setOtherOnline(null); setJoined(false); setError('')
    const connection = connectionRef.current
    async function load() {
      try {
        if (connectionStatus === 'connected' && connection?.state === HubConnectionState.Connected) {
          const online = await connection.invoke<boolean>('JoinConversation', selectedId)
          if (!cancelled) { setOtherOnline(online); setJoined(true) }
        }
        const history = await fetchJson<History>(`/api/chat/conversations/${selectedId}/messages`, token)
        if (!cancelled) {
          setMessages(current => {
            const merged = new Map([...history.items, ...current].map(message => [message.id, message]))
            return [...merged.values()].sort((a, b) => a.id - b.id)
          })
          setHasMore(history.hasMore)
          void refreshInbox()
        }
      } catch (e) { if (!cancelled) setError(e instanceof Error ? e.message : 'Could not open conversation.') }
      finally { if (!cancelled) setLoading(false) }
    }
    void load()
    return () => {
      cancelled = true
      if (connection?.state === HubConnectionState.Connected)
        void connection.invoke('LeaveConversation', selectedId).catch(() => {})
    }
  }, [token, selectedId, connectionStatus, refreshInbox])

  useEffect(() => {
    if (!token) return
    const open = (event: Event) => {
      const id = (event as CustomEvent<number>).detail
      if (!Number.isInteger(id)) return
      void refreshInbox().then(() => setSelectedId(id))
    }
    window.addEventListener('premscart-open-chat', open)
    return () => window.removeEventListener('premscart-open-chat', open)
  }, [token, refreshInbox])

  useEffect(() => { endRef.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }) }, [messages.length, selectedId])

  async function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const text = draft.trim()
    const connection = connectionRef.current
    if (!text || selectedId === null || !joined || connection?.state !== HubConnectionState.Connected) return
    setSending(true); setError('')
    try { await connection.invoke('SendMessage', selectedId, text); setDraft('') }
    catch (e) { setError(e instanceof Error ? e.message : 'Could not send message.') }
    finally { setSending(false) }
  }

  async function loadOlder() {
    if (selectedId === null || !messages.length) return
    setLoading(true); setError('')
    try {
      const result = await fetchJson<History>(`/api/chat/conversations/${selectedId}/messages?beforeId=${messages[0].id}`, token)
      setMessages(items => [...result.items.filter(old => !items.some(x => x.id === old.id)), ...items])
      setHasMore(result.hasMore)
    } catch (e) { setError(e instanceof Error ? e.message : 'Could not load older messages.') }
    finally { setLoading(false) }
  }

  const selected = conversations.find(x => x.id === selectedId)
  return <section className="chat-section" id="messages">
    <div className="section-heading"><div><p className="eyebrow">CAMPUS CONVERSATIONS</p><h2>Messages</h2></div>{token && <span className={`chat-connection ${connectionStatus}`}>{connectionStatus === 'connected' ? '● Live' : connectionStatus === 'reconnecting' ? 'Reconnecting…' : connectionStatus === 'connecting' ? 'Connecting…' : 'Offline'}</span>}</div>
    {!token ? <div className="empty-state"><h3>Sign in to read messages</h3><p>Start a conversation from a product listing.</p><a className="market-link" href="#account">Go to student sign in</a></div> : <>
      {error && <p className="form-error" role="alert">{error}{connectionStatus === 'offline' && <button className="chat-retry" onClick={() => setRetry(n => n + 1)}>Retry</button>}</p>}
      <div className={`chat-layout ${selectedId?'chat-has-selection':''}`}><aside className="chat-inbox"><h3>Conversations</h3><label className="chat-search"><span className="sr-only">Search conversations</span><input placeholder="Search conversations" value={query} onChange={e=>setQuery(e.target.value)}/></label><label className="check-field"><input type="checkbox" checked={onlyUnread} onChange={e=>setOnlyUnread(e.target.checked)}/>Unread only</label>{conversations.length === 0 ? <p className="chat-empty">No conversations yet. Open a product and select “Message seller.”</p> : conversations.filter(c=>(!onlyUnread||c.unreadCount>0)&&`${c.otherUserName} ${c.productTitle}`.toLowerCase().includes(query.toLowerCase())).map(c => <button className={`chat-conversation ${c.id === selectedId ? 'active' : ''}`} key={c.id} onClick={() => go(`/messages/${c.id}`)}><strong>{c.otherUserName}</strong>{c.unreadCount > 0 && <span className="unread-count">{c.unreadCount}</span>}<small>{c.productTitle}</small><span className="chat-preview">{c.lastMessage || 'Start the conversation'}</span></button>)}</aside><div className="chat-thread">{selected ? <><header className="chat-thread-header"><a className="chat-back icon-button" href="/messages" aria-label="Back to conversations"><Icon name="arrow"/></a><div><h3>{selected.otherUserName}</h3><p><a href={`/listings/${selected.productId}`}>{selected.productTitle}</a></p></div><span className={`presence ${otherOnline ? 'online' : ''}`}>{otherOnline === null ? 'Checking…' : otherOnline ? 'Online' : 'Offline'}</span></header><div className="chat-messages">{hasMore && <button className="text-button older-messages" disabled={loading} onClick={loadOlder}>Load older messages</button>}{loading && messages.length === 0 ? <p className="chat-empty">Loading messages…</p> : messages.length === 0 ? <p className="chat-empty">Say hello and ask about this product.</p> : messages.map(message => <div className={`chat-bubble ${message.senderId === myId ? 'mine' : ''}`} key={message.id}><p>{message.messageText}</p>{message.senderId!==myId&&<button className="text-button" onClick={()=>window.dispatchEvent(new CustomEvent('premscart-report',{detail:{messageId:message.id,name:'message'}}))}>Report</button>}<time dateTime={message.sentAt}>{new Date(message.sentAt).toLocaleString()}</time></div>)}<div ref={endRef} /></div><form className="chat-compose" onSubmit={send}><label className="sr-only" htmlFor="chat-draft">Message</label><input id="chat-draft" maxLength={2000} placeholder={joined ? 'Write a message…' : 'Connecting to chat…'} value={draft} onChange={e => setDraft(e.target.value)} disabled={!joined || sending} /><button className="market-action" type="submit" disabled={!draft.trim() || !joined || sending}>Send</button></form></> : <div className="chat-empty chat-select">Choose a conversation to view its messages.</div>}</div></div>
    </>}
  </section>
}
