#!/usr/bin/env python3
"""Execute the real, repeatable discount defect lifecycle through the running API."""
import argparse
import json
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--url', default='http://127.0.0.1:5080')
    parser.add_argument('--output', default='.tools/demo-evidence.json')
    args = parser.parse_args()

    def request(path, method='GET', body=None):
        data = json.dumps(body).encode() if body is not None else None
        req = Request(args.url.rstrip('/') + '/api' + path, data=data,
                      headers={'Content-Type': 'application/json'}, method=method)
        with urlopen(req, timeout=15) as response:
            return json.load(response)

    registry = request('/regression-registry')
    if not registry['isClean'] or len(registry['buildCommit']) != 40:
        raise RuntimeError('Use a clean Git checkout, rebuild the API, and restart it before verified resolution.')
    issue = request('/issues/104')

    def mutate(suffix, body, method='POST'):
        nonlocal issue
        request('/issues/104/' + suffix, method,
                {**body, 'version': issue['version']})
        issue = request('/issues/104')

    if issue['status'] == 'Resolved':
        mutate('status', {'status': 'Reopened', 'actorId': 1}, 'PATCH')
    if issue['status'] != 'InProgress':
        mutate('status', {'status': 'InProgress', 'actorId': 1}, 'PATCH')
    if issue['assigneeId'] is None:
        mutate('assignee', {'assigneeId': 2, 'actorId': 1}, 'PATCH')
    for definition in registry['tests'][:2]:
        if not any(t['testIdentifier'] == definition['identifier'] for t in issue['regressionTests']):
            mutate('regression-tests', {'testIdentifier': definition['identifier'], 'actorId': 1})
    mutate('fix-references', {
        'branchName': 'main', 'commitHash': registry['buildCommit'],
        'description': 'Running clean build containing the corrected discount calculator and shared regression assertions.',
        'actorId': 1,
    })
    boundary = next(t for t in issue['regressionTests'] if t['testIdentifier'] == 'discount.boundary')
    mutate(f"regression-tests/{boundary['id']}/executions", {'actorId': 1, 'defectiveFixture': True})
    before = next(t for t in issue['regressionTests'] if t['id'] == boundary['id'])['executions'][0]
    if before['result'] != 'Failed':
        raise RuntimeError('The defective boundary fixture unexpectedly passed.')
    print('BEFORE:', before['evidence'])
    for test_id in [t['id'] for t in issue['regressionTests']]:
        mutate(f'regression-tests/{test_id}/executions', {'actorId': 1, 'defectiveFixture': False})
        execution = next(t for t in issue['regressionTests'] if t['id'] == test_id)['executions'][0]
        if execution['result'] != 'Passed':
            raise RuntimeError('The corrected implementation failed an assertion.')
        print('AFTER:', execution['evidence'])
    mutate('status', {'status': 'InReview', 'actorId': 1}, 'PATCH')
    mutate('status', {'status': 'Resolved', 'actorId': 5,
                     'resolutionNote': 'The controlled boundary and full-discount assertions passed for the linked clean build. The defective boundary fixture failed as expected.'}, 'PATCH')
    if issue['status'] != 'Resolved' or issue['resolutionOverridden']:
        raise RuntimeError('Expected a verified resolution without override.')
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps({'registry': registry, 'before': before, 'issue': issue}, indent=2) + '\n')
    print(f"RESOLVED: PD-104, commit {registry['buildCommit']}, evidence: {output}")


if __name__ == '__main__':
    try:
        main()
    except HTTPError as error:
        raise SystemExit(f'API error {error.code}: {error.read().decode()}') from error
    except (URLError, RuntimeError) as error:
        raise SystemExit(str(error)) from error
