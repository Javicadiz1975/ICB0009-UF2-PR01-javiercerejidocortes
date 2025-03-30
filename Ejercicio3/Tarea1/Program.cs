using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    /**
     * Clase que extiende la información de un paciente
     * con datos temporales adicionales para la simulación.
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

    // Variables de sincronización y control
    static List<PacienteExtendido> colaEspera = new List<PacienteExtendido>();
    static List<PacienteExtendido> pacientesAtendidos = new List<PacienteExtendido>();
    static object lockCola = new object();
    static Semaphore semaforoMedicos = new Semaphore(4, 4);
    static Semaphore semaforoDiagnostico = new Semaphore(2, 2);
    static int turnoDiagnostico = 1;
    static object lockTurno = new object();
    static object lockEstadisticas = new object();

    // Control estadístico
    static int pacientesProcesados = 0;
    static TimeSpan totalUsoDiagnostico = TimeSpan.Zero;
    static DateTime inicioSimulacion;
    static DateTime finSimulacion;

    // Configuración general
    static int totalPacientesGenerados = 0;
    static bool ejecutar = true;
    static int LIMITE_PRUEBA = 100;

    /**
     * Método principal. Inicia la simulación completa del sistema hospitalario con pacientes infinitos.
     */
    static void Main(string[] args)
    {
        inicioSimulacion = DateTime.Now;
        Random random = new Random();

        // Hilo generador de pacientes cada 2 segundos
        Thread generador = new Thread(() =>
        {
            while (ejecutar && totalPacientesGenerados < LIMITE_PRUEBA)
            {
                Thread.Sleep(2000);

                var paciente = new PacienteExtendido
                {
                    Paciente = new Paciente(0, totalPacientesGenerados * 2, random.Next(5, 16)),
                    NumeroLlegada = ++totalPacientesGenerados,
                    HoraLlegada = DateTime.Now
                };

                lock (lockCola)
                {
                    colaEspera.Add(paciente);
                    Console.WriteLine($"🧍 Paciente {paciente.Paciente.Id} [#{paciente.NumeroLlegada}][PRIORIDAD {paciente.Paciente.Prioridad}] ha llegado. Estado: EsperaConsulta");
                }
            }
        });
        generador.Start();

        //  Hilo coordinador que gestiona la atención médica y el diagnóstico
        Thread hiloCoordinador = new Thread(() =>
        {
            while (true)
            {
                PacienteExtendido? siguiente = null;

                // Buscar al siguiente paciente por prioridad y orden
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

                    // Atender al paciente con un hilo del pool
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        siguiente.HoraInicioConsulta = DateTime.Now;
                        TimeSpan espera = siguiente.HoraInicioConsulta - siguiente.HoraLlegada;
                        Console.WriteLine($"🩺 Paciente {siguiente.Paciente.Id} [#{siguiente.NumeroLlegada}][PRIORIDAD {siguiente.Paciente.Prioridad}] entra a CONSULTA. Esperó: {espera.Seconds}s");

                        Thread.Sleep(siguiente.Paciente.TiempoConsulta * 1000);
                        siguiente.HoraFinConsulta = DateTime.Now;
                        semaforoMedicos.Release();

                        // Si requiere diagnóstico
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
                            // No necesita diagnóstico, pero avanza el turno igualmente
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

                        // ✅ Registrar estadísticas finales
                        lock (lockEstadisticas)
                        {
                            pacientesAtendidos.Add(siguiente);
                            pacientesProcesados++;
                        }

                        Console.WriteLine($"✅ Paciente {siguiente.Paciente.Id} [#{siguiente.NumeroLlegada}] finaliza atención.");
                    });
                }
                else
                {
                    Thread.Sleep(200);
                }

                // Condición de parada
                if (pacientesProcesados >= LIMITE_PRUEBA)
                {
                    ejecutar = false;
                    break;
                }
            }

            finSimulacion = DateTime.Now;
            MostrarEstadisticasFinales();
        });

        hiloCoordinador.Start();
        hiloCoordinador.Join();
        generador.Join();
    }

    /**
     * Muestra un resumen de estadísticas al finalizar la simulación.
     */
    static void MostrarEstadisticasFinales()
    {
        Console.WriteLine("\n📈 --- FIN DEL DÍA ---");

        var emergencias = pacientesAtendidos.Where(p => p.Paciente.Prioridad == 1).ToList();
        var urgencias = pacientesAtendidos.Where(p => p.Paciente.Prioridad == 2).ToList();
        var generales = pacientesAtendidos.Where(p => p.Paciente.Prioridad == 3).ToList();

        Console.WriteLine("🙍 Pacientes atendidos:");
        Console.WriteLine($"- 🚑 Emergencias: {emergencias.Count}");
        Console.WriteLine($"- 🏥 Urgencias: {urgencias.Count}");
        Console.WriteLine($"- 🩺 Consultas generales: {generales.Count}");

        double PromedioEspera(List<PacienteExtendido> lista)
        {
            if (lista.Count == 0) return 0;
            return lista.Average(p => (p.HoraInicioConsulta - p.HoraLlegada).TotalSeconds);
        }

        Console.WriteLine("\n⏱️Tiempo promedio de espera:");
        Console.WriteLine($"- 🚑 Emergencias: {Math.Round(PromedioEspera(emergencias))}s");
        Console.WriteLine($"- 🏥 Urgencias: {Math.Round(PromedioEspera(urgencias))}s");
        Console.WriteLine($"- 🩺 Consultas generales: {Math.Round(PromedioEspera(generales))}s");

        // ⏳ Cálculo del uso de diagnóstico
        TimeSpan duracionTotal = finSimulacion - inicioSimulacion;
        double tiempoDisponible = duracionTotal.TotalSeconds * 2;
        double porcentajeUso = totalUsoDiagnostico.TotalSeconds / tiempoDisponible * 100;

        Console.WriteLine($"\n⏱️Uso promedio de máquinas de diagnóstico: {Math.Round(porcentajeUso)}%");
    }
}
