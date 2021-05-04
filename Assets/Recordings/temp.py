import json
import sys

with open(sys.argv[1]) as f:
    j = json.load(f)
    prevTime = 0
    for i,line in enumerate(j['touchData']):
        td = line['timeSinceStart'] - prevTime
        prevTime = line['timeSinceStart']
        line['timeDelta'] = td
        j['touchData'][i] = line
    print json.dumps(j, indent=4, sort_keys=True)