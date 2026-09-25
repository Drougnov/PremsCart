"""Live shopping/rental integration checks. Run on a LOCAL API + Mailpit.
Creates disposable student accounts and listings, retaining the records.
"""
from smoke import account, call, post
from datetime import datetime, timedelta, timezone
import time

def run():
    _, _, seller, _ = account('rentalowner')
    _, _, buyer, _ = account('renter')
    places = call('/api/transactions/locations', token=buyer)
    assert {x['locationName'] for x in places} == {'Main gate', 'Canteen', 'Library'}
    category = call('/api/products/categories', token=seller)[0]['id']
    product = post('/api/products', dict(title='Rental integration calculator', description='A test rental.', categoryId=category,
        price=40, transactionType='Rent', condition='Good', status='Available', location='Library', isNegotiable=False), seller)['id']
    preferred = (datetime.now(timezone.utc) + timedelta(days=1)).date().isoformat()
    line = dict(productId=product, rentalDays=3, expectedUnitPrice=40, rentalStartDate=preferred)
    prefs = call('/api/users/preferences', token=buyer)
    assert all(prefs.values())
    call('/api/users/preferences', 'PUT', {**prefs, 'rentals':False}, buyer)
    assert not call('/api/users/preferences', token=buyer)['rentals']
    call('/api/cart/checkout', 'POST', {'items':[line]}, seller, status=409)
    call('/api/cart/checkout', 'POST', {'items':[{**line,'rentalDays':31}]}, buyer, status=409)
    call('/api/cart/checkout', 'POST', {'items':[{**line,'expectedUnitPrice':1}]}, buyer, status=409)
    quote = post('/api/cart/quote', {'items':[line]}, buyer)['items'][0]
    assert quote['available'] and quote['total'] == 120 and quote['rentalStartDate'] == preferred
    past = (datetime.now(timezone.utc)-timedelta(days=1)).date().isoformat()
    call('/api/cart/checkout', 'POST', {'items':[{**line,'rentalStartDate':past}]}, buyer, status=409)
    oid = post('/api/cart/checkout', {'items':[line]}, buyer)['orderIds'][0]
    call('/api/cart/checkout', 'POST', {'items':[line]}, buyer, status=409)
    call(f'/api/transactions/orders/{oid}/accept', 'POST', {}, buyer, status=403)
    post(f'/api/transactions/orders/{oid}/accept', {}, seller)
    conversation = post(f'/api/transactions/orders/{oid}/conversation', {}, buyer)['id']
    assert post(f'/api/transactions/orders/{oid}/conversation', {}, seller)['id'] == conversation
    assert not any(n['title']=='Order update' for n in call('/api/notifications', token=buyer)['items'])
    call('/api/users/preferences', 'PUT', prefs, buyer)
    pickup = datetime.now(timezone.utc) + timedelta(seconds=5)
    post(f'/api/transactions/orders/{oid}/pickup', {'locationId':places[0]['id'], 'pickupTime':pickup.isoformat().replace('+00:00','Z')}, buyer)
    call(f'/api/transactions/orders/{oid}/pickup/confirm', 'POST', {}, buyer, status=403)
    post(f'/api/transactions/orders/{oid}/pickup/confirm', {}, seller)
    # A counterproposal resets agreement and swaps who can agree.
    pickup = datetime.now(timezone.utc) + timedelta(seconds=5)
    post(f'/api/transactions/orders/{oid}/pickup', {'locationId':places[-1]['id'], 'pickupTime':pickup.isoformat().replace('+00:00','Z')}, seller)
    call(f'/api/transactions/orders/{oid}/pickup/confirm', 'POST', {}, seller, status=403)
    post(f'/api/transactions/orders/{oid}/pickup/confirm', {}, buyer)
    call(f'/api/transactions/orders/{oid}/complete', 'POST', {}, buyer, status=409)
    time.sleep(5.2)
    post(f'/api/transactions/orders/{oid}/complete', {}, buyer)
    order = next(x for x in call('/api/transactions/orders', token=buyer) if x['id']==oid)
    assert order['rentalStartDate']==preferred
    assert order['status']=='Rented' and order['rentalDueAt'] and not order['completedAt']
    call(f'/api/transactions/orders/{oid}/cancel', 'POST', {}, buyer, status=409)
    call('/api/community/reviews', 'POST', {'orderId':oid,'rating':5,'comment':'Too early'}, buyer, status=409)
    post(f'/api/transactions/orders/{oid}/return', {}, buyer)
    call(f'/api/transactions/orders/{oid}/return/confirm', 'POST', {}, buyer, status=403)
    post(f'/api/transactions/orders/{oid}/return/confirm', {}, seller)
    order = next(x for x in call('/api/transactions/orders', token=buyer) if x['id']==oid)
    assert order['status']=='Completed' and order['returnedAt']
    assert call(f'/api/products/{product}', token=buyer)['status']=='Available'
    post('/api/community/reviews', {'orderId':oid,'rating':5,'comment':'Returned safely'}, buyer)
    assert post('/api/cart/quote', {'items':[line]}, buyer)['items'][0]['available']
    print('PASS rental lifecycle, cart pricing/validation, role checks, return, review, and availability')

if __name__=='__main__': run()
