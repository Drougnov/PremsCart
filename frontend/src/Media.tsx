import { useEffect, useState } from 'react'
import Icon from './Icon'

export function PrivateImage({url,token,alt}:{url?:string|null;token:string;alt:string}){
 const [source,setSource]=useState(''),[failed,setFailed]=useState(false)
 useEffect(()=>{
  setSource('');setFailed(false);if(!url)return
  // Demo/showcase records can use public CC0 image URLs. <img> loading does not require CORS,
  // while app-owned /api/... images stay protected and are fetched with the auth token.
  if(/^https?:\/\//i.test(url)){setSource(url);return}
  let active=true,objectUrl='';const controller=new AbortController()
  fetch(url,{headers:{Authorization:`Bearer ${token}`},signal:controller.signal})
   .then(r=>{if(!r.ok)throw new Error('Image unavailable');return r.blob()})
   .then(blob=>{if(!active)return;objectUrl=URL.createObjectURL(blob);setSource(objectUrl)})
   .catch(()=>{if(active)setFailed(true)})
  return()=>{active=false;controller.abort();if(objectUrl)URL.revokeObjectURL(objectUrl)}
 },[url,token])
 return source?<img src={source} alt={alt} loading="lazy" referrerPolicy="no-referrer" onError={()=>{setSource('');setFailed(true)}}/>:<span className={`product-photo-placeholder ${url&&!failed?'image-loading':''}`} role="img" aria-label={url&&!failed?'Loading photo':`No photo for ${alt}`}><Icon name="package"/><small>{url&&!failed?'Loading photo…':'Photo not added'}</small></span>
}

export function FilePreview({file}:{file:File}){const [url,setUrl]=useState('');useEffect(()=>{const object=URL.createObjectURL(file);setUrl(object);return()=>URL.revokeObjectURL(object)},[file]);return url?<img src={url} alt={file.name}/>:null}
