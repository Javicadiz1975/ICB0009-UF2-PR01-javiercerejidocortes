using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    /**
     * Clase que almacena los datos extendidos del paciente,
     * incluyendo tiempos de llegada, consulta y diagnóstico.
     */
    class PacienteExtendido
    {
        public Paciente Paciente { get; set; } = null!;
        public int NumeroLlegada { get; set; }
        public DateTime HoraLlegada { get; set; }
        public DateTime HoraInicioConsulta { get; set; }
        public DateTime HoraFinConsulta { get; set; }
        public DateTime? HoraInicioDiagnostico { get; set; }
        public DateTime? HoraFinDiagnostico { get; set; }
    }

    // Lista de pacientes esperando ser atendidos
    static List<PacienteExtendido> colaEspera = new List<PacienteExtendido>();

    // Lista de pacientes ya atendidos
    static List<PacienteExtendido> pacientesAtendidos = new List<PacienteExtendido>();

    // Objetos para sincronización
    static object lockCola = new object();
    static object lockTurno = new object();
    static object lockEstadisticas = new object();

    // Recursos del hospital
    static Semaphore semaforoMedicos = new Semaphore(4, 4);         // 4 médicos
    static Semaphore semaforoDiagnostico = new Semaphore(2, 2);     // 2 máquinas

    static int turnoDiagnostico = 1;
    static int totalPacientes = 20;
    static int pacientesProcesados = 0;
    static TimeSpan totalUsoDiagnostico = TimeSpan.Zero;

    static DateTime inicioSimulacion;
    static DateTime finSimulacion;

    /**
     * Punto de entrada de la aplicación.
     * Crea 20 pacientes y simula su paso por consulta y diagnóstico.
     */
    static void Main(string[] args)
    {
        inicioSimulacion = DateTime.Now;
        Random random = new Random();
        List<Thread> hilosPacientes = new List<Thread>();

        // Generación de pacientes
        for (int i = 0; i < totalPacientes; i++)
        {
            int tiempoLlegada = i * 2;
            int llegada = i + 1;

            Thread hilo = new Thread(() =>
            {
                Thread.Sleep(tiempoLlegada * 1000);
                var paciente = new PacienteExtendido
                {
                    Paciente = new Paciente(0, tiempoLlegada, random.Next(5, 16)),
                    NumeroLlegada = llegada,
                    HoraLlegada = DateTime.Now
                };

                lock (lockCola)
                {
                    colaEspera.Add(paciente);
                    Console.WriteLine($"🧍 Paciente {paciente.Paciente.Id} [#{paciente.NumeroLlegada}][PRIORIDAD {paciente.Paciente.Prioridad}] ha llegado. .🛑 Estado: EsperaConsulta");
                }
            });

            hilo.Start();
            hilosPacientes.Add(hilo);
        }

        /**
         * Hilo coordinador que atiende a los pacientes según prioridad
         * y controla el acceso a médicos y diagnóstico.
         */
        Thread hiloCoordinador = new Thread(() =>
        {
            while (true)
            {
                PacienteExtendido? siguiente = null;

                lock (lockCola)
                {
                    siguiente = colaEspera
                        .OrderBy(p => p.Paciente.Prioridad)
                        .ThenBy(p => p.NumeroLlegada)
                        .FirstOrDefault();

                    if (siguiente != null)
                        colaEspera.Remove(siguiente);
                }

                if (siguiente != null)
                {
                    semaforoMedicos.WaitOne();

                    Thread hiloAtencion = new Thread(() =>
                    {
                        siguiente.HoraInicioConsulta = DateTime.Now;
                        TimeSpan espera = siguiente.HoraInicioConsulta - siguiente.HoraLlegada;
                        Console.WriteLine($"🩺 Paciente {siguiente.Paciente.Id} [#{siguiente.NumeroLlegada}][PRIORIDAD {siguiente.Paciente.Prioridad}] entra a CONSULTA. Esperó: {espera.Seconds}s");

                        Thread.Sleep(siguiente.Paciente.TiempoConsulta * 1000);
                        siguiente.HoraFinConsulta = DateTime.Now;
                        semaforoMedicos.Release();

                        // Diagnóstico si es necesario
                        if (siguiente.Paciente.RequiereDiagnostico)
                        {
                            Console.WriteLine($"🔜 Paciente {siguiente.Paciente.Id} espera DIAGNÓSTICO. Orden turno: {siguiente.NumeroLlegada}");

                            while (true)
                            {
                                lock (lockTurno)
                                {
                                    if (turnoDiagnostico == siguiente.NumeroLlegada)
                                    {
                                        semaforoDiagnostico.WaitOne();
                                        siguiente.HoraInicioDiagnostico = DateTime.Now;
                                        Console.WriteLine($"🔬 Paciente {siguiente.Paciente.Id} inicia DIAGNÓSTICO (15s)");
                                        Thread.Sleep(15000);
                                        siguiente.HoraFinDiagnostico = DateTime.Now;

                                        lock (lockEstadisticas)
                                            totalUsoDiagnostico += TimeSpan.FromSeconds(15);

                                        semaforoDiagnostico.Release();
                                        turnoDiagnostico++;
                                        break;
                                    }
                                }
                                Thread.Sleep(100);
                            }
                        }
                        else
                        {
                            // Paciente que no necesita diagnóstico espera su turno para avanzar
                            bool avanzandoTurno = false;
                            while (!avanzandoTurno)
                            {
                                lock (lockTurno)
                                {
                                    if (turnoDiagnostico == siguiente.NumeroLlegada)
                                    {
                                        turnoDiagnostico++;
                                        avanzandoTurno = true;
                                    }
                                }
                                Thread.Sleep(100);
                            }
                        }

                        // Añadir a estadísticas finales
                        lock (lockEstadisticas)
                        {
                            pacientesAtendidos.Add(siguiente);
                            pacientesProcesados++;
                        }

                        Console.WriteLine($"🔚 Paciente {siguiente.Paciente.Id} [#{siguiente.NumeroLlegada}] finaliza atención.");
                    });

                    hiloAtencion.Start();
                }
                else
                {
                    Thread.Sleep(200); // Espera hasta que haya paciente
                }

                if (pacientesProcesados >= totalPacientes)
                    break;
            }
        });

        hiloCoordinador.Start();

        // Esperar a que todos los pacientes lleguen
        foreach (var hilo in hilosPacientes)
            hilo.Join();

        hiloCoordinador.Join();

        finSimulacion = DateTime.Now;
        MostrarEstadisticasFinales();
    }

    /**
     * Muestra estadísticas al final de la simulación:
     * - Pacientes atendidos por prioridad
     * - Tiempo promedio de espera
     * - Uso de las máquinas de diagnóstico
     */
    static void MostrarEstadisticasFinales()
    {
        Console.WriteLine("\n📈 --- FIN DEL DÍA ---");

        var emergencias = pacientesAtendidos.Where(p => p.Paciente.Prioridad == 1).ToList();
        var urgencias = pacientesAtendidos.Where(p => p.Paciente.Prioridad == 2).ToList();
        var generales = pacientesAtendidos.Where(p => p.Paciente.Prioridad == 3).ToList();

        Console.WriteLine(" 🙍Pacientes atendidos:");
        Console.WriteLine($"- 🚑 Emergencias: {emergencias.Count}");
        Console.WriteLine($"- 🏥 Urgencias: {urgencias.Count}");
        Console.WriteLine($"- 🩺 Consultas generales: {generales.Count}");

        double PromedioEspera(List<PacienteExtendido> lista)
        {
            if (lista.Count == 0) return 0;
            return lista.Average(p => (p.HoraInicioConsulta - p.HoraLlegada).TotalSeconds);
        }

        Console.WriteLine("\nTiempo promedio de espera:");
        Console.WriteLine($"- 🚑 Emergencias: {Math.Round(PromedioEspera(emergencias))}s");
        Console.WriteLine($"- 🏥 Urgencias: {Math.Round(PromedioEspera(urgencias))}s");
        Console.WriteLine($"- 🩺 Consultas generales: {Math.Round(PromedioEspera(generales))}s");

        TimeSpan duracionTotal = finSimulacion - inicioSimulacion;
        double tiempoDisponible = duracionTotal.TotalSeconds * 2;
        double porcentajeUso = totalUsoDiagnostico.TotalSeconds / tiempoDisponible * 100;

        Console.WriteLine($"⏱️\nUso promedio de máquinas de diagnóstico: {Math.Round(porcentajeUso)}%");
    }
}
