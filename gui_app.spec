# -*- mode: python ; coding: utf-8 -*-


a = Analysis(
    ['F:\\自助旅游助手\\tools\\codex-migrate\\gui_app.py'],
    pathex=[],
    binaries=[],
    datas=[('F:\\ocr_envs\\py310\\Library\\lib\\tcl8.6', 'tcl\\tcl8.6'), ('F:\\ocr_envs\\py310\\Library\\lib\\tk8.6', 'tcl\\tk8.6')],
    hiddenimports=[],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=['F:\\自助旅游助手\\tools\\codex-migrate\\hook-tcl.py'],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='gui_app',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
