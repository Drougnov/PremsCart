import { useEffect, useState } from 'react'
export type CartLine = { productId:number; mode?:string; rentalDays?:number; rentalStartDate?:string }
const key=(userId:number)=>`premscart-cart-v1-${userId}`
export function readCart(userId:number):CartLine[] {
 try { const raw=JSON.parse(localStorage.getItem(key(userId))??'[]'); return Array.isArray(raw)?raw.filter(x=>Number.isInteger(x?.productId)&&x.productId>0).slice(0,20):[] } catch{return []}
}
export function writeCart(userId:number,items:CartLine[]){localStorage.setItem(key(userId),JSON.stringify(items));window.dispatchEvent(new Event('premscart-cart'))}
export function addToCart(userId:number,line:CartLine){const items=readCart(userId);const index=items.findIndex(x=>x.productId===line.productId);if(index<0){if(items.length>=20)throw new Error('Your cart holds up to 20 items. Check out or remove an item first.');items.push(line)}else items[index]=line;writeCart(userId,items)}
export function useCart(userId?:number){const [items,setItems]=useState<CartLine[]>([]);useEffect(()=>{const update=()=>setItems(userId?readCart(userId):[]);update();window.addEventListener('premscart-cart',update);window.addEventListener('storage',update);return()=>{window.removeEventListener('premscart-cart',update);window.removeEventListener('storage',update)}},[userId]);return items}
