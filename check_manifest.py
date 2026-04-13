import json
path=r'F:\??????\_pkg_tmp\manifest.json'
text=open(path,'r',encoding='utf-8').read()
obj=json.loads(text)
print(type(obj))
print(obj.keys())
print(type(obj['items']))
print(obj['items'][0].keys())
