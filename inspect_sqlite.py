import sqlite3
path=r'C:\Users\NewUser\.codex\state_5.sqlite'
conn=sqlite3.connect(path)
cur=conn.cursor()
print([r[0] for r in cur.execute("select name from sqlite_master where type='table' order by name")])
