import type { SVGProps } from 'react'

export type IconName =
  | 'copy' | 'clock' | 'check' | 'home' | 'market' | 'gift' | 'wanted' | 'store' | 'message' | 'search'
  | 'bell' | 'sun' | 'moon' | 'dashboard' | 'listing' | 'purchase' | 'sale'
  | 'offer' | 'heart' | 'star' | 'user' | 'lock' | 'shield' | 'settings'
  | 'plus' | 'arrow' | 'chevron' | 'menu' | 'close' | 'package' | 'inventory'

export default function Icon({ name, ...props }: { name: IconName } & SVGProps<SVGSVGElement>) {
  const common = { fill: 'none', stroke: 'currentColor', strokeWidth: 1.9, strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const }
  return <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false" {...props} {...common}>
    {name === 'copy' && <><rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V4a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1v11a1 1 0 0 0 1 1h4"/></>}
    {name === 'clock' && <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>}
    {name === 'check' && <path d="m5 12 4 4L19 6"/>}
    {name === 'home' && <><path d="M3 10.8 12 3l9 7.8"/><path d="M5.5 9.7V21h13V9.7"/><path d="M9.5 21v-6h5v6"/></>}
    {name === 'market' && <><path d="M4 9h16l-1.2 11H5.2L4 9Z"/><path d="M7 9V7a5 5 0 0 1 10 0v2"/></>}
    {name === 'gift' && <><path d="M3 10h18v11H3z"/><path d="M12 10v11M2 7h20v3H2z"/><path d="M12 7H8.8a2.3 2.3 0 1 1 2.2-3c.5 1.4 1 3 1 3Zm0 0h3.2a2.3 2.3 0 1 0-2.2-3c-.5 1.4-1 3-1 3Z"/></>}
    {name === 'wanted' && <><circle cx="11" cy="11" r="6"/><path d="m16 16 5 5M11 8v6M8 11h6"/></>}
    {name === 'store' && <><path d="M4 10v10h16V10"/><path d="M3 5h18l-1 5H4L3 5Z"/><path d="M8 20v-5h8v5"/></>}
    {name === 'message' && <><path d="M4 5h16v12H9l-5 4V5Z"/><path d="M8 9h8M8 13h5"/></>}
    {name === 'search' && <><circle cx="11" cy="11" r="6.5"/><path d="m16 16 4.5 4.5"/></>}
    {name === 'bell' && <><path d="M6 9a6 6 0 0 1 12 0c0 7 3 7 3 7H3s3 0 3-7"/><path d="M10 20h4"/></>}
    {name === 'sun' && <><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></>}
    {name === 'moon' && <path d="M20.5 14.5A8.5 8.5 0 0 1 9.5 3.5 8.5 8.5 0 1 0 20.5 14.5Z"/>}
    {name === 'dashboard' && <><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></>}
    {name === 'listing' && <><path d="M6 3h12v18H6z"/><path d="M9 8h6M9 12h6M9 16h4"/></>}
    {name === 'purchase' && <><path d="M3 5h2l2 11h11l2-7H6"/><circle cx="9" cy="20" r="1"/><circle cx="17" cy="20" r="1"/></>}
    {name === 'sale' && <><path d="M4 4h6l10 10-6 6L4 10V4Z"/><circle cx="8" cy="8" r="1"/></>}
    {name === 'offer' && <><path d="M12 3v18M16 7.5c-.8-1.1-2-1.7-4-1.7-2.2 0-3.8 1.1-3.8 2.8 0 4.3 7.6 1.7 7.6 6.2 0 1.9-1.7 3.3-4.2 3.3-2 0-3.5-.6-4.6-2"/></>}
    {name === 'heart' && <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z"/>}
    {name === 'star' && <path d="m12 3 2.8 5.7 6.2.9-4.5 4.4 1.1 6.2-5.6-2.9-5.6 2.9 1.1-6.2L3 9.6l6.2-.9L12 3Z"/>}
    {name === 'user' && <><circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/></>}
    {name === 'lock' && <><rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3"/></>}
    {name === 'shield' && <path d="M12 3 4.5 6v5.5c0 4.6 3.1 7.5 7.5 9.5 4.4-2 7.5-4.9 7.5-9.5V6L12 3Z"/>}
    {name === 'settings' && <><circle cx="12" cy="12" r="3"/><path d="M19 13.5v-3l2-1-2-3.4-2.2.5-2.6-1.5L13.5 3h-3l-.7 2.1-2.6 1.5L5 6.1 3 9.5l2 1v3l-2 1 2 3.4 2.2-.5 2.6 1.5.7 2.1h3l.7-2.1 2.6-1.5 2.2.5 2-3.4-2-1Z"/></>}
    {name === 'plus' && <path d="M12 5v14M5 12h14"/>}
    {name === 'arrow' && <><path d="M5 12h14"/><path d="m14 7 5 5-5 5"/></>}
    {name === 'chevron' && <path d="m9 6 6 6-6 6"/>}
    {name === 'menu' && <path d="M4 7h16M4 12h16M4 17h16"/>}
    {name === 'close' && <path d="m6 6 12 12M18 6 6 18"/>}
    {name === 'package' && <><path d="m12 3 8 4-8 4-8-4 8-4Z"/><path d="M4 7v10l8 4 8-4V7M12 11v10"/></>}
    {name === 'inventory' && <><rect x="4" y="4" width="16" height="16" rx="2"/><path d="M8 9h8M8 13h8M8 17h5"/></>}
  </svg>
}
