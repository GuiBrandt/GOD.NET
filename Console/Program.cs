using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GOD;

namespace GOD_Console
{
    class Program
    {
        internal static Regex COMMAND_REGEX = new Regex(@"^\s*(\S+)(?:\s+(\S+))?(?:\s+(\S+))*\s*$");
        private static MemoryChecker checker;
        private static Random rnd = new Random();

        /// <summary>
        /// Nomes de autores
        /// </summary>
        static string[] AUTORES = new string[] {
            "Clarice Lispector",
            "Albert Einstein",
            "Miyamoto Musashi",
            "Friedrich Nietzsche",
            "Ernö Rubik",
            "Sigmund Freud",
            "Jamal",
            "Paulo Vitor",
            "Sakamoto"
        };

        /// <summary>
        /// Frases legais
        /// </summary>
        static string[] FRASES = new string[] {
            "Moe. Moe. Kyun.",
            "BIIIIRLLLLL",
            "Essas citações falsas são demais",
            "Olha como vem",
            "Eles estão te vendo",
            "Vai todo mundo pro SOE!",
            "Façam lição de casa",
            "Tô vendo essa zoeira...",
            "Eu quero café!",
            "Ja se inscreveu no INOVA JOVEM?",
            "Quando eu for telado, eu quero virar um pinguim"
        };

        /// <summary>
        /// Ponto de entrada
        /// </summary>
        /// <param name="args">Argumentos da linha de comando</param>
        static void Main(string[] args)
        {
            Console.WriteLine("GOD.Net v1.0.0");
            Console.WriteLine("\"{0}\" ~ {1}", RandomPhrase(), RandomAuthor());
            Console.WriteLine();

            checker = new MemoryChecker(
                GOD.Properties.Settings.Default.MonitoredProcess,
                GOD.Properties.Settings.Default.MemoryUsageLimit
            );

            Console.WriteLine("Monitorando " + checker.ProcessName);
            Console.WriteLine("Limite de memória: " + checker.MemoryUsageLimit + "K");
            Console.WriteLine("Taxa de atualização: " + (1000 / checker.Interval) + "Hz");
            Console.WriteLine();

            checker.Start();

            checker.OnDanger += Checker_OnDanger;
            checker.OnSafe += Checker_OnSafe;

            while (true) {
                Console.Write("> ");
                string cmd = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(cmd))
                    continue;

                try
                {
                    Match match = COMMAND_REGEX.Match(cmd);
                    if (!match.Success)
                        Console.WriteLine("Comando inválido. Digite help ou ? para ajuda.");
                    else
                        ExecuteCommand(match);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Deu ruim: " + e.Message);
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Executa um comando a partir de um match de regex
        /// </summary>
        /// <param name="regMatch">Match de regex</param>
        private static void ExecuteCommand(Match regMatch)
        {
            switch (regMatch.Groups[1].Value)
            {
                case "help":
                case "?":
                    Console.WriteLine("A ajuda ainda não está disponível :(");
                    break;

                case "exit":
                case "bye":
                case "bai":
                    checker.Stop();
                    Environment.Exit(0);
                    break;

                case "msg":
                    string msg = regMatch.Groups[0].Value.Substring(regMatch.Groups[1].Length + 1);
                    Task.Run(() => MessageBox.Show(new Form() { TopMost = true }, msg, "GOD"));
                    break;

                case "warn":
                case "!":
                    msg = regMatch.Groups[0].Value.Substring(regMatch.Groups[1].Length + 1);
                    Task.Run(() => MessageBox.Show(new Form() { TopMost = true }, msg, "GOD", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                    break;

                case "info":
                case "i":
                    msg = regMatch.Groups[0].Value.Substring(regMatch.Groups[1].Length + 1);
                    Task.Run(() => MessageBox.Show(new Form() { TopMost = true }, msg, "GOD", MessageBoxButtons.OK, MessageBoxIcon.Information));
                    break;

                #region Configuração

                case "set_process":
                case "sp":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                    {
                        checker.ProcessName = regMatch.Groups[2].Value;
                        GOD.Properties.Settings.Default.MonitoredProcess = checker.ProcessName;

                        Console.WriteLine("Monitorando " + checker.ProcessName);
                    }
                    break;

                case "set_memory":
                case "sm":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum valor foi especificado");
                    else
                        try
                        {
                            checker.MemoryUsageLimit = int.Parse(regMatch.Groups[2].Value);
                            GOD.Properties.Settings.Default.MemoryUsageLimit = checker.MemoryUsageLimit;

                            Console.WriteLine("Limite de memória: " + checker.MemoryUsageLimit + "K");
                        }
                        catch
                        {
                            Console.WriteLine("Valor inválido");
                        }
                    break;

                case "set_frequency":
                case "sf":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum valor foi especificado");
                    else
                        try
                        {
                            int freq = int.Parse(regMatch.Groups[2].Value);
                            checker.Interval = 1000 / freq;
                            GOD.Properties.Settings.Default.RefreshRate = freq;

                            Console.WriteLine("Taxa de atualização: " + freq + "Hz");
                        }
                        catch
                        {
                            Console.WriteLine("Valor inválido");
                        }
                    break;

                case "save_settings":
                case "ss":
                    Properties.Settings.Default.Save();
                    break;

                #endregion

                #region Controle de processos

                case "kill":
                case "k":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                        UnlimitedPower.Kill(regMatch.Groups[2].Value);

                    break;

                case "hide":
                case "h":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                        UnlimitedPower.Hide(regMatch.Groups[2].Value);

                    break;

                case "unhide":
                case "uh":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                        UnlimitedPower.Unhide(regMatch.Groups[2].Value);

                    break;

                case "minimize":
                case "m":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                        UnlimitedPower.Minimize(regMatch.Groups[2].Value);

                    break;

                case "show":
                case "s":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                        UnlimitedPower.Show(regMatch.Groups[2].Value);

                    break;

                case "enum":
                case "e":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                        foreach (int pid in UnlimitedPower.Enumerate(regMatch.Groups[2].Value))
                            Console.WriteLine("PID {0}", pid);
                    break;

                case "enum_windows":
                case "ew":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                        foreach (IntPtr hwnd in UnlimitedPower.GetWindowHandles(regMatch.Groups[2].Value))
                        {
                            uint pid;
                            UnlimitedPower.GetWindowThreadProcessId(hwnd, out pid);
                            Console.WriteLine("HWND {0} (PID {1})", hwnd, pid);
                        }
                    break;

                #endregion

                #region Window Styles

                case "styles":
                    Console.WriteLine("WS_OVERLAPPED    00000000");
                    Console.WriteLine("WS_TABSTOP       00010000");
                    Console.WriteLine("WS_MINIMIZEBOX   00020000");
                    Console.WriteLine("WS_THICKFRAME    00040000");
                    Console.WriteLine("WS_SYSMENU       00080000");
                    Console.WriteLine("WS_HSCROLL       00100000");
                    Console.WriteLine("WS_VSCROLL       00200000");
                    Console.WriteLine("WS_DLGFRAME      00400000");
                    Console.WriteLine("WS_BORDER        00800000");
                    Console.WriteLine("WS_CAPTION       00C00000");
                    Console.WriteLine("WS_MAXIMIZE      01000000");
                    Console.WriteLine("WS_CLIPCHILDREN  02000000");
                    Console.WriteLine("WS_CLIPSIBLINGS  04000000");
                    Console.WriteLine("WS_DISABLED      08000000");
                    Console.WriteLine("WS_VISIBLE       10000000");
                    Console.WriteLine("WS_MINIMIZE      20000000");
                    Console.WriteLine("WS_CHILD         40000000");
                    Console.WriteLine("WS_POPUP         80000000");
                    break;

                case "get_window_style":
                case "gws":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                        Console.WriteLine(UnlimitedPower.GetWindowStyle(regMatch.Groups[2].Value).ToString("X"));

                    break;

                case "add_window_style":
                case "aws":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else if (regMatch.Groups.Count <= 3)
                        Console.WriteLine("Nenhum estilo foi especificado");
                    else
                        UnlimitedPower.AddWindowStyle(regMatch.Groups[2].Value, uint.Parse(regMatch.Groups[3].Value, NumberStyles.HexNumber));

                    break;

                case "remove_window_style":
                case "rws":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else if (regMatch.Groups.Count <= 3)
                        Console.WriteLine("Nenhum estilo foi especificado");
                    else
                        UnlimitedPower.RemoveWindowStyle(regMatch.Groups[2].Value, uint.Parse(regMatch.Groups[3].Value, NumberStyles.HexNumber));

                    break;

                #endregion

                #region Window Styles Ex

                case "styles_ex":
                    Console.WriteLine("WS_EX_LEFT                   00000000");
                    Console.WriteLine("WS_EX_DLGMODALFRAME          00000001");
                    Console.WriteLine("WS_EX_NOPARENTNOTIFY         00000004");
                    Console.WriteLine("WS_EX_TOPMOST                00000008");
                    Console.WriteLine("WS_EX_ACCEPTFILES            00000010");
                    Console.WriteLine("WS_EX_TRANSPARENT            00000020");
                    Console.WriteLine("WS_EX_MDICHILD               00000040");
                    Console.WriteLine("WS_EX_TOOLWINDOW             00000080");
                    Console.WriteLine("WS_EX_WINDOWEDGE             00000100");
                    Console.WriteLine("WS_EX_CLIENTEDGE             00000200");
                    Console.WriteLine("WS_EX_CONTEXTHELP            00000400");
                    Console.WriteLine("WS_EX_RIGHT                  00001000");
                    Console.WriteLine("WS_EX_RTLREADING             00002000");
                    Console.WriteLine("WS_EX_LEFTSCROLLBAR          00004000");
                    Console.WriteLine("WS_EX_CONTROLPARENT          00010000");
                    Console.WriteLine("WS_EX_STATICEDGE             00020000");
                    Console.WriteLine("WS_EX_APPWINDOW              00040000");
                    Console.WriteLine("WS_EX_LAYERED                00080000");
                    Console.WriteLine("WS_EX_NOINHERITLAYOUT        00100000");
                    Console.WriteLine("WS_EX_NOREDIRECTIONBITMAP    00200000");
                    Console.WriteLine("WS_EX_LAYOUTRTL              00400000");
                    Console.WriteLine("WS_EX_COMPOSITED             02000000");
                    Console.WriteLine("WS_EX_NOACTIVATE             08000000");
                    break;

                case "get_window_style_ex":
                case "gwsx":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else
                        Console.WriteLine(UnlimitedPower.GetWindowStyleEx(regMatch.Groups[2].Value).ToString("X"));

                    break;

                case "add_window_style_ex":
                case "awsx":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else if (regMatch.Groups.Count <= 3)
                        Console.WriteLine("Nenhum estilo foi especificado");
                    else
                        UnlimitedPower.AddWindowStyleEx(regMatch.Groups[2].Value, uint.Parse(regMatch.Groups[3].Value, NumberStyles.HexNumber));

                    break;

                case "remove_window_style_ex":
                case "rwsx":
                    if (regMatch.Groups.Count <= 2)
                        Console.WriteLine("Nenhum processo foi especificado");
                    else if (regMatch.Groups.Count <= 3)
                        Console.WriteLine("Nenhum estilo foi especificado");
                    else
                        UnlimitedPower.RemoveWindowStyleEx(regMatch.Groups[2].Value, uint.Parse(regMatch.Groups[3].Value, NumberStyles.HexNumber));

                    break;

                #endregion

                default:
                    Console.WriteLine("Comando inválido. Digite help ou ? para ajuda.");
                    break;
            }
        }

        /// <summary>
        /// Obtém o nome de um autor aleatório
        /// </summary>
        /// <returns>Um nome aleatório de autor</returns>
        private static string RandomAuthor()
        {
            return AUTORES[rnd.Next(AUTORES.Length)];
        }

        /// <summary>
        /// Obtém uma frase aleatória
        /// </summary>
        /// <returns>Uma frase aleatória</returns>
        private static string RandomPhrase()
        {
            return FRASES[rnd.Next(FRASES.Length)];
        }

        /// <summary>
        /// Evento disparado quando deu ruim
        /// </summary>
        /// <param name="sender">Processo que disparou o evento</param>
        private static void Checker_OnDanger(object sender, EventArgs e)
        {
            Console.WriteLine("RUUUUUUUUUUUUUUUUUUUN MOTHERFUCKER");

            using (StreamReader reader = new StreamReader("ruuun.god"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        Match match = COMMAND_REGEX.Match(line);
                        if (!match.Success)
                            Console.WriteLine("Comando inválido. Digite help ou ? para ajuda.");
                        else
                            ExecuteCommand(match);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Deu ruim: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Evento disparado quando a situação de acalma
        /// </summary>
        private static void Checker_OnSafe(object sender, EventArgs e)
        {
            Console.WriteLine("It's safe now");

            using (StreamReader reader = new StreamReader("safe.god"))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        Match match = COMMAND_REGEX.Match(line);
                        if (!match.Success)
                            Console.WriteLine("Comando inválido. Digite help ou ? para ajuda.");
                        else
                            ExecuteCommand(match);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Deu ruim: " + ex.Message);
                    }
                }
            }
        }
    }
}
