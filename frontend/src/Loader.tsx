import { useEffect, useState, type ReactNode } from 'react'

/** Render only while an actual request is pending. No artificial delay. */
export default function Loader({label='Loading your campus…',onRetry}:{label?:string;onRetry?:()=>void}) {
 const [slow,setSlow]=useState(false)
 useEffect(()=>{const timer=setTimeout(()=>setSlow(true),12000);return()=>clearTimeout(timer)},[])
 return <section className="pc-loader"><div className="pc-loader-mark" aria-hidden="true"><span className="pc-loader-ring"/><img src="/brand/premscart-logo.jpeg" alt="" width="72" height="72"/><span className="pc-loader-star">✦</span></div><p className="pc-loader-brand">PremsCart</p><p className="pc-loader-label" role="status" aria-live="polite">{slow?'Taking a little longer. The server may be waking up.':label}</p><div className="pc-loader-dots" aria-hidden="true"><i/><i/><i/></div>{slow&&onRetry&&<button type="button" className="pc-loader-retry" onClick={onRetry}>Retry</button>}</section>
}

export function BusyLabel({busy,children}:{busy:boolean;children:ReactNode}) {
 return <span className="pc-busy-label">{busy&&<span className="pc-button-spinner" aria-hidden="true"/>}{children}</span>
}
