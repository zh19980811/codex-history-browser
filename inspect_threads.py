import sqlite3
path=r'C:\Users\NewUser\.codex\state_5.sqlite'
conn=sqlite3.connect(path)
cur=conn.cursor()
cur.execute("PRAGMA table_info(threads)")
print(cur.fetchall())
