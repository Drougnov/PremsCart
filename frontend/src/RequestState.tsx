import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api } from './api'
type Request={id:number;productId:number;status:string;transactionType?:string}
type State={orders:Request[];offers:Request[];userId?:number}
const Context=createContext<State>({orders:[],offers:[]})
export const requestsChanged=()=>window.dispatchEvent(new Event('premscart-requests-change'))
export function RequestProvider({token,userId,children}:{token:string;userId?:number;children:ReactNode}){
 const [data,setData]=useState<State>({orders:[],offers:[]})
 useEffect(()=>{let live=true;setData({orders:[],offers:[]});if(!token||!userId)return;const load=()=>Promise.all([api<Request[]>('/api/transactions/orders'),api<Request[]>('/api/transactions/offers')]).then(([orders,offers])=>{if(live)setData({userId,orders:orders.filter((o:any)=>o.buyerId===userId),offers:offers.filter((o:any)=>o.buyerId===userId)})}).catch(()=>{});void load();const refresh=()=>{if(document.visibilityState==='visible')void load()};window.addEventListener('premscart-requests-change',refresh);window.addEventListener('focus',refresh);const timer=setInterval(refresh,15000);return()=>{live=false;clearInterval(timer);window.removeEventListener('premscart-requests-change',refresh);window.removeEventListener('focus',refresh)}},[token,userId])
 return <Context.Provider value={data.userId===userId?data:{orders:[],offers:[]}}>{children}</Context.Provider>
}
export function useProductRequest(id:number){const data=useContext(Context);const order=data.orders.find(o=>o.productId===id&&['Pending','Accepted','Pickup scheduled','Rented','Return requested'].includes(o.status));if(order)return {label:order.status==='Pending'?'Request sent':order.status==='Rented'?'On rent':order.status==='Return requested'?'Return requested':'View your order',href:`/orders/${order.id}`};const offer=data.offers.find(o=>o.productId===id&&['Pending','Countered'].includes(o.status));return offer?{label:'Offer sent',href:'/dashboard/offers'}:null}
