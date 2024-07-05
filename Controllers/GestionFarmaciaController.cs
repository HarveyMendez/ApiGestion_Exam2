using ApiCliente;
using ApiGestion.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace ApiGestion.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GestionFarmaciaController : ControllerBase
    {
        private readonly Utils _utils;

        public GestionFarmaciaController(Utils utils)
        {
            _utils = utils;
        }

        [HttpGet("/GestionarSolicitudesFarmacia")]
        public IActionResult GestionarSolicitudesFarmacia()
        {
            List<SolicitudDTO> solicitudesFarmacia = new List<SolicitudDTO>();
            using (SqlConnection conn = _utils.GetConnectionModuloCitas())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("sp_obtener_recetas_pendientes", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    solicitudesFarmacia.Add(new SolicitudDTO
                    {
                        id_receta = (int)dr["id_receta"],
                        medicamentos = (string)dr["medicamentos"],
                        correo_paciente = (string)dr["correo_paciente"]
                    });
                }
                conn.Close();
            }
            RegistrarSolicitudesFarmacia(solicitudesFarmacia);

            return Ok();
        }

        [HttpGet("/GestionarSolicitudesPendientes")]
        public IActionResult GestionarSolicitudesPendientes()
        {
            List<int> solicitudesPendientes = new List<int>();
            using (SqlConnection conn = _utils.GetConnectionModuloFarmacia())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("sp_obtener_solicitudes_pendientes", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    solicitudesPendientes.Add((int)dr["id_receta"]);
                }
                conn.Close();
            }
            MarcarSolicitudesProcesadas(solicitudesPendientes);

            return Ok();
        }

        [HttpGet("/GestionarNotificaciones")]
        public IActionResult GestionarNotificaciones()
        {
            List<EmailDTO> notificaciones = new List<EmailDTO>();
            using (SqlConnection conn = _utils.GetConnectionModuloFarmacia())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("sp_obtener_notificaciones_pendientes", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    notificaciones.Add(new EmailDTO
                    {
                        Id = (int)dr["IdNotificacion"],
                        Subject = (string)dr["Asunto"],
                        Body = (string)dr["Cuerpo"],
                        AddressTo = (string)dr["Correo"]
                    });
                }
                conn.Close();
            }
            EnviarCorreos(notificaciones);

            return Ok();
        }

        private void MarcarSolicitudesProcesadas(List<int> solicitudesPendientes)
        {
            using (SqlConnection conn = _utils.GetConnectionModuloFarmacia())
            {
                conn.Open();

                foreach (var solicitud in solicitudesPendientes)
                {
                    SqlCommand cmd = new SqlCommand("sp_marcar_solicitud_procesada", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // Agregar parámetros al comando
                    cmd.Parameters.AddWithValue("@id_receta", solicitud);

                    // Ejecutar el comando
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }

            using (SqlConnection conn = _utils.GetConnectionModuloCitas())
            {
                conn.Open();

                foreach (var solicitud in solicitudesPendientes)
                {
                    SqlCommand cmd = new SqlCommand("sp_marcar_solicitud_notificada", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // Agregar parámetros al comando
                    cmd.Parameters.AddWithValue("@id_receta", solicitud);

                    // Ejecutar el comando
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }
        }

        private void RegistrarSolicitudesFarmacia(List<SolicitudDTO> solicitudesFarmacia)
        {
            using (SqlConnection conn = _utils.GetConnectionModuloFarmacia())
            {
                conn.Open();

                foreach (var solicitud in solicitudesFarmacia)
                {
                    SqlCommand cmd = new SqlCommand("sp_registrar_solicitud", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // Agregar parámetros al comando
                    cmd.Parameters.AddWithValue("@id_receta", solicitud.id_receta);
                    cmd.Parameters.AddWithValue("@medicamentos", solicitud.medicamentos);
                    cmd.Parameters.AddWithValue("@correo_paciente", solicitud.correo_paciente);

                    // Ejecutar el comando
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }
            MarcarSolicitudesRegistradas(solicitudesFarmacia);
        }

        private void MarcarSolicitudesRegistradas(List<SolicitudDTO> solicitudesFarmacia)
        {
            using (SqlConnection conn = _utils.GetConnectionModuloCitas())
            {
                conn.Open();

                foreach (var solicitud in solicitudesFarmacia)
                {
                    SqlCommand cmd = new SqlCommand("sp_marcarSolicitud_registrada", conn);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // Agregar parámetros al comando
                    cmd.Parameters.AddWithValue("@id_receta", solicitud.id_receta);

                    // Ejecutar el comando
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }
        }

        private void MarcarNotificacionEnviada(int idNotificacion)
        {
            using (SqlConnection conn = _utils.GetConnectionModuloFarmacia())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("sp_marcar_notificacion_enviada", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@IdNotificacion", idNotificacion);

                SqlDataReader dr = cmd.ExecuteReader();
                conn.Close();
            }
        }

        private async void EnviarCorreos(List<EmailDTO> notificaciones)
        {
            foreach (EmailDTO notificacion in notificaciones)
            {
                using HttpResponseMessage response = await _utils.GetAPIHost().PostAsJsonAsync(_utils.GetEmailAPI(), notificacion);
                if (response.IsSuccessStatusCode)
                {
                    MarcarNotificacionEnviada(notificacion.Id);
                }
            }
        }


    }
}
