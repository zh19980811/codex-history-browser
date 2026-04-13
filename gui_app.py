import os
import subprocess
import threading
import tkinter as tk
from tkinter import messagebox, scrolledtext


def run_cmd(cmd_path, log):
    def worker():
        if not os.path.exists(cmd_path):
            messagebox.showerror('Error', f'Not found: {cmd_path}')
            return
        log.insert(tk.END, f'Running: {cmd_path}\n')
        log.see(tk.END)
        try:
            proc = subprocess.run(['cmd', '/c', cmd_path], capture_output=True, text=True)
            if proc.stdout:
                log.insert(tk.END, proc.stdout + '\n')
            if proc.stderr:
                log.insert(tk.END, proc.stderr + '\n')
            log.see(tk.END)
            if proc.returncode == 0:
                messagebox.showinfo('Done', 'Operation completed successfully.')
            else:
                messagebox.showerror('Failed', f'Command failed with code {proc.returncode}.')
        except Exception as e:
            messagebox.showerror('Error', str(e))
    threading.Thread(target=worker, daemon=True).start()


def main():
    exe_dir = os.path.dirname(os.path.abspath(__file__))
    export_cmd = os.path.join(exe_dir, 'export.cmd')
    import_cmd = os.path.join(exe_dir, 'import.cmd')

    root = tk.Tk()
    root.title('Codex History Migration')
    root.geometry('520x360')

    frm = tk.Frame(root, padx=12, pady=12)
    frm.pack(fill=tk.BOTH, expand=True)

    tk.Label(frm, text='Codex History Migration', font=('Segoe UI', 12, 'bold')).pack(anchor='w')
    tk.Label(frm, text='Choose an action:', font=('Segoe UI', 10)).pack(anchor='w', pady=(6, 8))

    btn_frame = tk.Frame(frm)
    btn_frame.pack(anchor='w')

    tk.Button(btn_frame, text='Export (source)', width=18, command=lambda: run_cmd(export_cmd, log)).pack(side=tk.LEFT, padx=(0, 8))
    tk.Button(btn_frame, text='Import (target)', width=18, command=lambda: run_cmd(import_cmd, log)).pack(side=tk.LEFT)

    tk.Label(frm, text='Log:', font=('Segoe UI', 10)).pack(anchor='w', pady=(12, 4))
    log = scrolledtext.ScrolledText(frm, height=10)
    log.pack(fill=tk.BOTH, expand=True)

    tk.Label(frm, text='Note: Close Codex before importing.', font=('Segoe UI', 9)).pack(anchor='w', pady=(8, 0))

    root.mainloop()


if __name__ == '__main__':
    main()
