import os
import sys

base = getattr(sys, '_MEIPASS', None)
if base:
    tcl = os.path.join(base, 'tcl', 'tcl8.6')
    tk = os.path.join(base, 'tcl', 'tk8.6')
    os.environ['TCL_LIBRARY'] = tcl
    os.environ['TK_LIBRARY'] = tk
