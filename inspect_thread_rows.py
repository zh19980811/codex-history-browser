import sqlite3
path=r'C:\Users\NewUser\.codex\state_5.sqlite'
conn=sqlite3.connect(path)
cur=conn.cursor()
cur.execute('select id, title, created_at, updated_at from threads order by updated_at desc limit 3')
for row in cur.fetchall():
    print(row)
