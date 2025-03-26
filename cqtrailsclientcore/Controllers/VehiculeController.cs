using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cqtrailsclientcore.Context;
using cqtrailsclientcore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cqtrailsclientcore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculeController : ControllerBase
    {
        private readonly MyDbContext _db;

        public VehiculeController(MyDbContext db)
        {
            _db = db;
        }

        // GET: api/Vehicule
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehiculo>>> GetAllVehiculos()
        {
            try
            {
                var vehiculos = await _db.Vehiculos.ToListAsync();
                return Ok(vehiculos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: api/Vehicule/capacidad/{capacidad}
        [HttpGet("capacidad/{capacidad}")]
        public async Task<ActionResult<IEnumerable<Vehiculo>>> GetVehiculosByCapacidad(int capacidad)
        {
            try
            {
                var vehiculos = await _db.Vehiculos
                    .Where(v => v.Capacidad == capacidad)
                    .ToListAsync();

                if (!vehiculos.Any())
                {
                    return NotFound($"No se encontraron vehículos con capacidad: {capacidad}");
                }

                return Ok(vehiculos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: api/Vehicule/capacidades
        [HttpGet("capacidades")]
        public async Task<ActionResult<IEnumerable<int>>> GetAllCapacidades()
        {
            try
            {
                var capacidades = await _db.Vehiculos
                    .Select(v => v.Capacidad)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(capacidades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: api/Vehicule/marcas
        [HttpGet("marcas")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllMarcas()
        {
            try
            {
                // Asumiendo que la marca está en el campo TipoVehiculo
                // Si tienes un campo específico para marca, deberías usarlo en su lugar
                var marcas = await _db.Vehiculos
                    .Select(v => v.TipoVehiculo)
                    .Distinct()
                    .ToListAsync();

                return Ok(marcas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: api/Vehicule/modelos/{marca}
        [HttpGet("modelos/{marca}")]
        public async Task<ActionResult<IEnumerable<string>>> GetModelosByMarca(string marca)
        {
            try
            {
                var modelos = await _db.Vehiculos
                    .Where(v => v.TipoVehiculo == marca)
                    .Select(v => v.Modelo)
                    .Distinct()
                    .ToListAsync();

                if (!modelos.Any())
                {
                    return NotFound($"No se encontraron modelos para la marca: {marca}");
                }

                return Ok(modelos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: api/Vehicule/anos/{marca}/{modelo}
        [HttpGet("anos/{marca}/{modelo}")]
        public async Task<ActionResult<IEnumerable<int>>> GetAnosByMarcaAndModelo(string marca, string modelo)
        {
            try
            {
                var anos = await _db.Vehiculos
                    .Where(v => v.TipoVehiculo == marca && v.Modelo == modelo)
                    .Select(v => v.Ano)
                    .Distinct()
                    .OrderByDescending(a => a)
                    .ToListAsync();

                if (!anos.Any())
                {
                    return NotFound($"No se encontraron años para la marca {marca} y modelo {modelo}");
                }

                return Ok(anos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: api/Vehicule/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehiculo>> GetVehiculoById(int id)
        {
            try
            {
                var vehiculo = await _db.Vehiculos.FindAsync(id);

                if (vehiculo == null)
                {
                    return NotFound($"No se encontró el vehículo con ID: {id}");
                }

                return Ok(vehiculo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
