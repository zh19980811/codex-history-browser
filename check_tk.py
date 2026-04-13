import tkinter as tk
import os
print('TclVersion', tk.TclVersion)
print('TkVersion', tk.TkVersion)
print('TCL_LIBRARY', os.environ.get('TCL_LIBRARY'))
print('TK_LIBRARY', os.environ.get('TK_LIBRARY'))
print('tk file', tk.__file__)
