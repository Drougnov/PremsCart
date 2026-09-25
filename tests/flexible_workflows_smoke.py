"""Run against local API + Mailpit: python3 tests/flexible_workflows_smoke.py.
Creates separate verified demo users and listings; requires a running updated backend.
"""
from smoke import account, call, post
from datetime import datetime, timedelta, timezone
import time

def run():
    _, _, seller, sid = account('flexseller')
    _, _, buyer, bid = account('flexbuyer')
    _, _, other, _ = account('flexother')
    category = call('/api/products/categories', token=seller)[0]['id']
    location = call('/api/transactions/locations', token=seller)[0]
    listing = dict(title='Flexible study kit', description='Sale or daily rental test', categoryId=category,
                   price=500, transactionType='Sell', allowRent=True, rentalPrice=25,
                   condition='Good', status='Available', location=location['locationName'], isNegotiable=True)
    call('/api/products', 'POST', {**listing, 'rentalPrice': 0}, seller, status=400)
    pid = post('/api/products', listing, seller)['id']
    for mode, days, unit, total in [('Sell', None, 500, 500), ('Rent', 3, 25, 75)]:
        line = dict(productId=pid, mode=mode, rentalDays=days)
        quote = post('/api/cart/quote', dict(items=[line]), buyer)['items'][0]
        assert quote['available'] and quote['transactionType'] == mode
        assert quote['unitPrice'] == unit and quote['total'] == total
    wrong = post('/api/cart/quote', dict(items=[dict(productId=pid, mode='Giveaway')]), buyer)['items'][0]
    assert not wrong['available']
    rent = dict(productId=pid, mode='Rent', rentalDays=3, expectedUnitPrice=25)
    oid = post('/api/cart/checkout', dict(items=[rent]), buyer)['orderIds'][0]
    call('/api/cart/checkout', 'POST', dict(items=[rent]), buyer, status=409)
    competitor = post('/api/transactions/orders', dict(productId=pid, mode='Sell'), other)['id']
    call('/api/community/reviews', 'POST', dict(orderId=oid, rating=5, comment='Too early'), buyer, status=409)
    post(f'/api/transactions/orders/{oid}/accept', {}, seller)
    assert next(o for o in call('/api/transactions/orders', token=other) if o['id'] == competitor)['status'] == 'Cancelled'
    assert call(f'/api/products/{pid}', token=buyer)['status'] == 'Reserved'
    pickup = datetime.now(timezone.utc) + timedelta(seconds=4)
    post(f'/api/transactions/orders/{oid}/pickup', dict(locationId=location['id'], pickupTime=pickup.isoformat().replace('+00:00', 'Z')), buyer)
    post(f'/api/transactions/orders/{oid}/pickup/confirm', {}, seller)
    call(f'/api/transactions/orders/{oid}/complete', 'POST', {}, buyer, status=409)
    time.sleep(4.2)
    post(f'/api/transactions/orders/{oid}/complete', {}, buyer)
    order = next(o for o in call('/api/transactions/orders', token=buyer) if o['id'] == oid)
    assert order['transactionType'] == 'Rent' and order['status'] == 'Rented' and order['finalPrice'] == 75
    post(f'/api/transactions/orders/{oid}/return', {}, buyer)
    post(f'/api/transactions/orders/{oid}/return/confirm', {}, seller)
    assert call(f'/api/products/{pid}', token=buyer)['status'] == 'Available'
    for token in [buyer, seller]:
        post('/api/community/reviews', dict(orderId=oid, rating=5, comment='Easy campus handoff.'), token)
        assert next(o for o in call('/api/transactions/orders', token=token) if o['id'] == oid)['hasReviewed']
    call('/api/community/reviews', 'POST', dict(orderId=oid, rating=5, comment='Duplicate'), buyer, status=409)
    sale = post('/api/transactions/orders', dict(productId=pid, mode='Sell'), buyer)['id']
    assert next(o for o in call('/api/transactions/orders', token=buyer) if o['id'] == sale)['transactionType'] == 'Sell'
    post(f'/api/transactions/orders/{sale}/cancel', {}, buyer)
    wanted = dict(title='Need a study kit', description='Buy, rent or a giveaway is fine.', budget=600, categoryId=category, status='Open')
    wid = post('/api/wanted', wanted, buyer)['id']
    response = post(f'/api/wanted/{wid}/respond', dict(productId=pid), seller)
    assert response['conversationId'] > 0
    assert post(f'/api/wanted/{wid}/respond', dict(productId=pid), seller)['conversationId'] == response['conversationId']
    call(f'/api/wanted/{wid}/respond', 'POST', dict(productId=pid), other, status=400)
    call(f'/api/wanted/{wid}/respond', 'POST', dict(productId=pid), buyer, status=400)
    call(f'/api/wanted/{wid}', 'PUT', {**wanted, 'status': 'Fulfilled'}, buyer)
    call(f'/api/wanted/{wid}/respond', 'POST', dict(productId=pid), seller, status=404)
    print('PASS combined pricing, checkout, exclusive reservation, rental return, purchase choice, bilateral reviews, wanted response ownership/deduplication/closed-post checks')

if __name__ == '__main__':
    run()
