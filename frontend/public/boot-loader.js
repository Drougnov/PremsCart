// Runs before the React bundle. Once React mounts, it replaces this markup.
try { document.documentElement.classList.toggle('dark',localStorage.getItem('theme')==='dark'); } catch {}
const boot=document.getElementById('premscart-boot');
if(boot){
 const retry=boot.querySelector('button');
 retry?.addEventListener('click',()=>location.reload());
 setTimeout(()=>{
  if(!boot.isConnected)return;
  const status=boot.querySelector('[role="status"]');
  if(status)status.textContent='Taking a little longer. Check your connection and try again.';
  if(retry)retry.hidden=false;
 },12000);
}
