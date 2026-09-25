"""Run against a LOCAL running API + Mailpit. Creates unique demo data; no deletes.
Python 3.10+, standard library only. ADMIN_EMAIL/PASSWORD enable admin checks.
"""
import json, os, re, secrets, time, urllib.request, urllib.error
from datetime import datetime, timedelta, timezone
BASE=os.getenv('API_URL','http://localhost:5000')
MAIL=os.getenv('MAILPIT_URL','http://localhost:8025')
def call(path, method='GET', body=None, token=None, status=None):
    headers={'Content-Type':'application/json'}
    if token:headers['Authorization']='Bearer '+token
    request=urllib.request.Request(BASE+path,json.dumps(body).encode() if body is not None else None,headers,method=method)
    try:
        with urllib.request.urlopen(request,timeout=20) as r:code=r.status;raw=r.read()
    except urllib.error.HTTPError as e:code=e.code;raw=e.read()
    if status is not None:assert code==status,(path,code,raw)
    else:assert 200<=code<300,(path,code,raw)
    return json.loads(raw) if raw else {}
def code_for(email,purpose):
    for _ in range(30):
        with urllib.request.urlopen(MAIL+'/api/v1/messages') as r:messages=json.load(r)['messages']
        for m in messages:
            if purpose in m['Subject'] and any(a['Address'].lower()==email for a in m['To']):
                with urllib.request.urlopen(MAIL+'/api/v1/message/'+m['ID']) as r:detail=json.load(r)
                found=re.search(r'\b\d{6}\b',detail.get('Text',''))
                if found:return found.group()
        time.sleep(.3)
    raise AssertionError('Email not found in Mailpit: '+email)
def account(label):
    email=label+secrets.token_hex(4)+'_44009@bscse.puc.ac.bd';password='CampusDemo!123'
    call('/api/auth/register','POST',dict(firstName=label,lastName='Test',email=email,password=password))
    otp=code_for(email,'verification');call('/api/auth/verify-email','POST',dict(email=email,code=otp))
    login=call('/api/auth/login','POST',dict(email=email,password=password));return email,password,login['token'],login['profile']['id']
def post(path,body,token):return call(path,'POST',body,token)
def run():
    a,pa,ta,aid=account('seller');b,pb,tb,bid=account('buyer');c,pc,tc,cid=account('other')
    category=call('/api/products/categories',token=ta)[0]['id'];location=call('/api/transactions/locations',token=ta)[0]
    def listing(kind):return post('/api/products',dict(title='Smoke '+kind,description='Created by local smoke test',categoryId=category,price=200,transactionType=kind,condition='Good',status='Available',location=location['locationName'],isNegotiable=kind=='Sell'),ta)['id']
    def handoff(oid):
        pickup=datetime.now(timezone.utc)+timedelta(seconds=3)
        post(f'/api/transactions/orders/{oid}/pickup',dict(locationId=location['id'],pickupTime=pickup.isoformat().replace('+00:00','Z')),tb)
        call(f'/api/transactions/orders/{oid}/pickup/confirm','POST',{},tb,status=403)
        post(f'/api/transactions/orders/{oid}/pickup/confirm',{},ta)
        time.sleep(3.1)
        post(f'/api/transactions/orders/{oid}/complete',{},tb)
    pid=listing('Giveaway');post('/api/wishlist/'+str(pid),{},tb)
    oid=post('/api/transactions/orders',dict(productId=pid),tb)['id'];other=post('/api/transactions/orders',dict(productId=pid),tc)['id']
    call(f'/api/transactions/orders/{oid}/accept','POST',{},tc,status=403)
    post(f'/api/transactions/orders/{oid}/accept',{},ta)
    assert next(x for x in call('/api/transactions/orders',token=tc) if x['id']==other)['status']=='Cancelled'
    handoff(oid)
    assert next(x for x in call('/api/products/mine',token=ta) if x['id']==pid)['status']=='GivenAway'
    rid=post('/api/community/reviews',dict(orderId=oid,rating=5,comment='Smooth campus handoff.'),tb)['id']
    post('/api/community/reports',dict(reviewId=rid,reason='Test report for moderation'),ta)
    public=call('/api/users/'+str(aid),token=tb);assert public['givenAway']>=1 and 'universityEmail' not in public['profile']
    pid=listing('Sell');fid=post('/api/transactions/offers',dict(productId=pid,amount=150),tb)['id']
    call(f'/api/transactions/offers/{fid}/accept','POST',{},tb,status=403)
    post(f'/api/transactions/offers/{fid}/counter',dict(amount=180),ta)
    post(f'/api/transactions/offers/{fid}/counter',dict(amount=170),tb)
    offers=call('/api/transactions/offers',token=ta);assert len(next(o for o in offers if o['id']==fid)['history'])==3
    oid=post(f'/api/transactions/offers/{fid}/accept',{},ta)['orderId'];handoff(oid)
    assert next(x for x in call('/api/products/mine',token=ta) if x['id']==pid)['status']=='Sold'
    notices=call('/api/notifications',token=tb);assert notices['unread']>0
    call('/api/notifications/'+str(notices['items'][0]['id'])+'/read','POST',{},tc,status=404)
    post('/api/notifications/read-all',{},tb);assert call('/api/notifications',token=tb)['unread']==0
    post('/api/auth/forgot-password',dict(email=b));reset=code_for(b,'password reset')
    new='ChangedCampus!456';post('/api/auth/reset-password',dict(email=b,code=reset,password=new))
    call('/api/users/profile',token=tb,status=401)
    tb=post('/api/auth/login',dict(email=b,password=new))['token']
    post('/api/auth/change-password',dict(currentPassword=new,password='ChangedAgain!789'),tb)
    call('/api/users/profile',token=tb,status=401)
    call('/api/admin/dashboard',token=ta,status=403)
    if os.getenv('ADMIN_PASSWORD'):
        admin=post('/api/auth/login',dict(email=os.getenv('ADMIN_EMAIL','admin@premscart.local'),password=os.environ['ADMIN_PASSWORD']))['token']
        post('/api/moderator/users/'+str(cid)+'/action',dict(action='Suspend',reason='Smoke test suspension'),admin)
        call('/api/users/profile',token=tc,status=401)
        post('/api/moderator/users/'+str(cid)+'/action',dict(action='Reactivate',reason='Smoke test reactivation'),admin)
        post('/api/auth/login',dict(email=c,password=pc))
        name='Smoke category '+secrets.token_hex(3);post('/api/admin/lookups/categories',dict(name=name),admin)
        categories=call('/api/admin/lookups/categories',token=admin);row=next(x for x in categories if x['name']==name)
        call('/api/admin/lookups/categories/'+str(row['id']),'DELETE',token=admin)
    print('PASS: accounts, access controls, giveaway, competing requests, offer history, pickup agreement, completion, reviews, notifications, password reset/change'+(', admin/suspension' if os.getenv('ADMIN_PASSWORD') else '; admin checks skipped (set ADMIN_PASSWORD)'))
if __name__=='__main__':run()
