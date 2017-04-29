using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace GOD
{
    /// <summary>
    /// Classe usada para controle de processos
    /// </summary>
    public static class UnlimitedPower
    {
        // Constantes da Win32
        const int GWL_STYLE = -16;
        const int GWL_EXSTYLE = -20;
        
        const uint WS_EX_TOOLWINDOW = 0x00000080;
        const uint WS_EX_APPWINDOW = 0x00040000;
        const uint WS_VISIBLE = 0x10000000;

        const int SW_HIDE = 0;
        const int SW_NORMAL = 1;
        const int SW_MINIMIZE = 6;

        /// <summary>
        /// Dicionário com as janelas escondidas para cada processo
        /// </summary>
        private static Dictionary<string, List<IntPtr>> hiddenWindows = new Dictionary<string, List<IntPtr>>();

        /// <summary>
        /// Define a forma de exibição de uma janela
        /// </summary>
        /// <param name="hwnd">Handle da janela</param>
        /// <param name="nCmdShow">Comando de exibição</param>
        /// <returns>0 se a janela estivesse escondida e 1 se não</returns>
        [DllImport("user32")]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);
        
        /// <summary>
        /// Callback para a função EnumChildWindows
        /// </summary>
        /// <param name="hwnd">Handle de janela</param>
        /// <param name="lParam">Parâmetros</param>
        /// <returns>True se deve continuar a enumeração e falso caso contrário</returns>
        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        /// <summary>
        /// Executa um callback para cada janela
        /// </summary>
        /// <param name="callback">Callback a executar</param>
        /// <param name="lParam">Parâmetros</param>
        /// <returns>O valor de retorno é irrelevante</returns>
        [DllImport("user32")]
        public static extern int EnumWindows(Win32Callback callback, IntPtr lParam);

        /// <summary>
        /// Obtém o id do processo dono de uma janela
        /// </summary>
        /// <param name="hWnd">Janela</param>
        /// <param name="lpdwProcessId">Saída para o ID do processo</param>
        /// <returns>ID da thread da janela</returns>
        [DllImport("user32")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Obtém um long de uma janela
        /// </summary>
        /// <param name="hWnd">Handle de uma janela</param>
        /// <param name="nIndex">Índice do long</param>
        /// <returns>O long obtido</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Define um long de uma janela
        /// </summary>
        /// <param name="hWnd">Handle da janela</param>
        /// <param name="nIndex">Índice do long</param>
        /// <param name="dwNewLong">Valor a ser colocado no long</param>
        /// <returns>1 em caso de sucesso e 0 caso contrário</returns>
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);

        /// <summary>
        /// Termina todos os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        public static void Kill(string pname)
        {
            foreach (Process p in Process.GetProcessesByName(pname))
                p.Kill();
        }

        /// <summary>
        /// Enumera os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        public static int[] Enumerate(string pname)
        {
            List<int> pids = new List<int>();
            foreach (Process p in Process.GetProcessesByName(pname))
                pids.Add(p.Id);

            return pids.ToArray();
        }

        /// <summary>
        /// Esconde as janelas de todos os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        public static void Hide(string pname)
        {
            IntPtr[] handles = GetWindowHandles(pname);
            foreach (IntPtr hwnd in handles)
            {
                uint style = GetWindowLong(hwnd, GWL_STYLE);
                if ((style & WS_VISIBLE) == 0)
                    continue;
                
                ShowWindow(hwnd, SW_HIDE);

                if (!hiddenWindows.ContainsKey(pname))
                    hiddenWindows.Add(pname, new List<IntPtr>());
                hiddenWindows[pname].Add(hwnd);
            }
        }

        /// <summary>
        /// Obtém o handle de todas as janelas para um processo
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        /// <returns>Um array de handles de janelas do processo</returns>
        public static IntPtr[] GetWindowHandles(string pname)
        {
            // Lista os PIDs dos processos com o nome dado
            List<int> pids = new List<int>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(string.Format("SELECT ProcessID FROM Win32_Process WHERE Name LIKE '{0}.exe'", pname));
            ManagementObjectCollection pcs = searcher.Get();
            foreach (ManagementObject p in pcs)
                pids.Add((int)(uint)p["ProcessID"]);
            searcher.Dispose();
            pcs.Dispose();

            // Lista as janelas que batem com os PIDs listados
            List<IntPtr> windows = new List<IntPtr>();
            EnumWindows((IntPtr hwnd, IntPtr lParam) => {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);

                if (pids.Contains((int)pid))
                    windows.Add(hwnd);

                return true;
            }, IntPtr.Zero);

            return windows.ToArray();
        }

        /// <summary>
        /// Esconde as janelas de todos os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        public static void Unhide(string pname)
        {
            if (!hiddenWindows.ContainsKey(pname)) return;
            foreach (IntPtr hwnd in hiddenWindows[pname])
                ShowWindow(hwnd, 7);
        }

        /// <summary>
        /// Minimiza a janela de todos os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        public static void Minimize(string pname)
        {
            foreach (IntPtr hwnd in GetWindowHandles(pname))
                ShowWindow(hwnd, SW_MINIMIZE);
        }

        /// <summary>
        /// Mostra as janelas de todos os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        public static void Show(string pname)
        {
            if (!hiddenWindows.ContainsKey(pname)) return;
            foreach (IntPtr hwnd in hiddenWindows[pname])
                ShowWindow(hwnd, 8);
        }

        /// <summary>
        /// Adiciona um estilo na janela de todos os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        /// <param name="s">Estilo</param>
        public static void AddWindowStyle(string pname, uint s)
        {
            foreach (IntPtr hwnd in GetWindowHandles(pname))
                SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) | s);
        }

        /// <summary>
        /// Remove um estilo na janela de todos os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        /// <param name="s">Estilo</param>
        public static void RemoveWindowStyle(string pname, uint s)
        {
            foreach (IntPtr hwnd in GetWindowHandles(pname))
                SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~s);
        }

        /// <summary>
        /// Obtém o estilo da janela de um processo com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        /// <param name="s">Estilo</param>
        public static uint GetWindowStyle(string pname)
        {
            foreach (IntPtr hwnd in GetWindowHandles(pname))
                return GetWindowLong(hwnd, GWL_STYLE);
            return 0;
        }

        /// <summary>
        /// Adiciona um estilo na janela de todos os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        /// <param name="s">Estilo</param>
        public static void AddWindowStyleEx(string pname, uint s)
        {
            foreach (IntPtr hwnd in GetWindowHandles(pname))
                SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | (uint)s);
        }

        /// <summary>
        /// Remove um estilo na janela de todos os processos com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        /// <param name="s">Estilo</param>
        public static void RemoveWindowStyleEx(string pname, uint s)
        {
            foreach (IntPtr hwnd in GetWindowHandles(pname))
                SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) & ~(uint)s);
        }

        /// <summary>
        /// Obtém o estilo da janela de um processo com um determinado nome
        /// </summary>
        /// <param name="pname">Nome do processo</param>
        /// <param name="s">Estilo</param>
        public static uint GetWindowStyleEx(string pname)
        {
            foreach (IntPtr hwnd in GetWindowHandles(pname))
                return GetWindowLong(hwnd, GWL_EXSTYLE);
            return 0;
        }
    }
}
