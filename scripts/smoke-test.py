#!/usr/bin/env python3
"""Exercise a running Development demo API; never touches a live cloud store."""
import json
import sys
import uuid
import urllib.error
import urllib.request

BASE = sys.argv[1] if len(sys.argv) > 1 else 'http://localhost:5180'
checks = 0

def request(path, method='GET', data=None, token=None, expect=200):
    global checks
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(BASE + path, headers=headers, method=method,
                                 data=None if data is None else json.dumps(data).encode())
    try:
        with urllib.request.urlopen(req) as response:
            status, body = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, body = error.code, error.read()
    assert status == expect, f'{method} {path}: expected {expect}, got {status}: {body[:200]}'
    checks += 1
    return json.loads(body) if body and status < 400 else None

# The script refuses to continue unless the server confirms a Development demo.
demo = request('/api/Auth/Demo', 'POST')
token = demo['token']
assert request('/api/Auth/Me', token=token)['demo'] is True
request('/api/agent/runs', expect=401)
request('/api/agent/runs', 'POST', {'workflow': 'intake_draft', 'notes': 'test'}, expect=401)
records = request('/api/BlotterRecords', token=token)
assert records
source = records[0]
run = request('/api/agent/runs', 'POST', {'workflow': 'incident_summary', 'recordId': source['recordId']}, token)
assert run['status'] == 'needs_review' and run['result']['sourceRecordIds'] == [source['recordId']]
request('/api/agent/runs/' + run['id'] + '/review', 'POST', {'decision': 'accepted'}, token)
request('/api/agent/runs/' + run['id'] + '/review', 'POST', {'decision': 'rejected'}, token, expect=409)
assert request('/api/BlotterRecords/' + str(source['recordId']), token=token) == source
intake = request('/api/agent/runs', 'POST', {'workflow': 'intake_draft', 'notes': 'Synthetic intake. A resident reported a missing bag. The location is unverified.'}, token)
assert intake['status'] == 'needs_review' and intake['result']['sourceRecordIds'] == []
assert request('/api/agent/runs/' + intake['id'], token=token)['sourceNotes'] == 'Synthetic intake. A resident reported a missing bag. The location is unverified.'
request('/api/agent/runs/' + intake['id'] + '/review', 'POST', {'decision': 'accepted'}, token)
request('/api/agent/runs', 'POST', {'workflow': 'unsupported'}, token, expect=400)
request('/api/agent/runs', 'POST', {'workflow': 'intake_draft', 'notes': ''}, token, expect=400)
# A newly registered officer has neither access to the demo records nor its runs.
name = 'smoke.' + uuid.uuid4().hex[:10]
password = uuid.uuid4().hex + '!a1'
request('/api/Auth/Register', 'POST', {'username': name, 'password': password, 'badgeNumber': name,
                                     'firstName': 'Synthetic', 'lastName': 'Test'})
other = request('/api/Auth/Login', 'POST', {'username': name, 'password': password})['token']
assert request('/api/BlotterRecords', token=other) == []
assert request('/api/agent/runs', token=other) == []
request('/api/BlotterRecords/' + str(source['recordId']), token=other, expect=404)
request('/api/agent/runs/' + run['id'], token=other, expect=404)
request('/api/agent/runs/' + run['id'] + '/review', 'POST', {'decision': 'accepted'}, other, expect=404)
blocked = request('/api/agent/runs', 'POST', {'workflow': 'incident_summary', 'recordId': source['recordId']}, other)
assert blocked['status'] == 'failed' and blocked['result'] is None
request('/api/BlotterRecords', 'POST', {'categoryId': 1, 'location': 'Synthetic', 'narrative': 'Synthetic'}, other, expect=400)
created = request('/api/BlotterRecords', 'POST', {'categoryId': 1, 'location': 'Synthetic test location',
                  'narrative': 'Synthetic smoke test incident.', 'incidentDate': '2026-11-24T08:00:00Z'}, other, expect=201)
assert created['status'] == 'Pending'
assert len(request('/api/BlotterRecords', token=other)) == 1
assert request('/api/agent/runs/' + run['id'], token=token)['status'] == 'accepted'
print(f'Passed {checks} API smoke checks: auth, scope isolation, drafts, immutable reviews and explicit filing.')
