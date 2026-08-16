import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

const token = process.argv[2]
const segundos = Number(process.argv[3] ?? 25)

const conexion = new HubConnectionBuilder()
  .withUrl('http://127.0.0.1:5080/hubs/notificaciones', { accessTokenFactory: () => token })
  .configureLogging(LogLevel.Error)
  .build()

conexion.on('notificacion', (n) => {
  console.log('  >>> NOTIFICACION RECIBIDA EN VIVO')
  console.log('      tipo    :', n.tipo)
  console.log('      titulo  :', n.titulo)
  console.log('      mensaje :', n.mensaje)
  console.log('      paciente:', n.pacienteNombre, '|', n.modalidad, '|', n.hospitalNombre)
})

await conexion.start()
console.log('  cliente conectado al hub')

setTimeout(async () => {
  await conexion.stop()
  console.log('  cliente desconectado')
  process.exit(0)
}, segundos * 1000)
