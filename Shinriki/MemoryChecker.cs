using System;
using System.Threading.Tasks;
using System.Management;
using System.Threading;

using static System.Management.ManagementObjectCollection;

namespace GOD
{
    /// <summary>
    /// Verificador de memória, monitora os processos e dispara eventos quando o uso de memória deles
    /// passa da margem de segurança
    /// </summary>
    public class MemoryChecker
    {
        /// <summary>
        /// Nome do processo monitorado
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// Limite para o uso de memória considerado seguro em KB
        /// </summary>
        public int MemoryUsageLimit { get; set; }

        /// <summary>
        /// Intervalo entre as iterações do verificador em milissegundos
        /// </summary>
        public int Interval { get; set; } = 1000 / Properties.Settings.Default.RefreshRate;

        /// <summary>
        /// Número de processos detectados
        /// </summary>
        public int DetectedProcessesCount { get; private set; } = 0;

        /// <summary>
        /// Evento disparado quando a memória de algum dos processo monitorados passa do seguro
        /// </summary>
        public event EventHandler OnDanger;

        /// <summary>
        /// Evento disparado quando, depois de disparado um evento de perigo, a memória de mais nenhum processo
        /// fica além do valor considerado alarmante
        /// </summary>
        public event EventHandler OnSafe;

        /// <summary>
        /// Thread principal do verificador
        /// </summary>
        private Task _mainTask;

        /// <summary>
        /// Flag que sinaliza quando a thread principal deve rodar ou não
        /// </summary>
        private bool _run = false;

        /// <summary>
        /// Flag que sinaliza quando o evento de perigo foi disparado e um de segurança ainda não
        /// </summary>
        private bool _inDanger = false;

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="pname">Nome do processo a ser monitorado</param>
        /// <param name="mem">Uso de memória considerado seguro</param>
        public MemoryChecker(string pname, int mem)
        {
            ProcessName = pname;
            MemoryUsageLimit = mem;

            _mainTask = new Task(MainProc);
        }

        /// <summary>
        /// Começa o processo de verificação
        /// </summary>
        public void Start()
        {
            _run = true;
            _mainTask.Start();
        }

        /// <summary>
        /// Para o processo de verificação
        /// </summary>
        public void Stop()
        {
            _run = false;
            _mainTask.Wait();
            _mainTask.Dispose();
        }

        /// <summary>
        /// Estrutura de processo monitorado
        /// </summary>
        public struct Process
        {
            public int ProcessId;
            public int WorkingSet;
        }

        /// <summary>
        /// Procedimento principal
        /// </summary>
        private void MainProc()
        {
            while (_run)
            {
                using (ManagementObjectSearcher query = new ManagementObjectSearcher("SELECT name, processId, workingSetSize FROM win32_process"))
                using (ManagementObjectCollection processes = query.Get())
                {
                    DetectedProcessesCount = processes.Count;
                    ManagementObjectEnumerator enumerator = processes.GetEnumerator();

                    // Procura por processos com uso de memória alarmante
                    int pid = 0, workingSet = 0;
                    bool wellFuck = false;
                    while (!wellFuck && enumerator.MoveNext())
                    {
                        string name = enumerator.Current["name"].ToString();
                        if (!name.Equals(ProcessName, StringComparison.InvariantCultureIgnoreCase))
                            continue;

                        pid = int.Parse(enumerator.Current["processId"].ToString());
                        workingSet = int.Parse(enumerator.Current["workingSetSize"].ToString());

                        if (workingSet >= MemoryUsageLimit * 1024)
                            wellFuck = true;
                    }

                    // Se não estiver em estado de alerta e houver algum processo fora do comum,
                    // lança o evento de aviso
                    if (wellFuck && !_inDanger)
                    {
                        _inDanger = true;
                        OnDanger(new Process() { ProcessId = pid, WorkingSet = workingSet }, EventArgs.Empty);
                    }

                    // Se estiver em estado de alerta e tudo estiver normal, lança o evento de
                    // notificação de segurança
                    else if (!wellFuck && _inDanger)
                    {
                        _inDanger = false;
                        OnSafe(null, EventArgs.Empty);
                    }
                }

                Thread.Sleep(Interval);
            }
        }
    }
}
