// ... existing code ...
            // Obtener los items del carrito
            var detallesCarrito = await _db.DetalleCarrito
                .Include(d => d.Vehiculo)
                .Include(d => d.CiudadInicio)
                .Include(d => d.CiudadFin)
                .Where(d => d.CarritoId == carrito.id)
                .ToListAsync();

            if (detallesCarrito == null || !detallesCarrito.Any())
            {
                return NotFound("No hay items en el carrito para crear una reservación");
            }

            // Usar fechas sin especificar Kind para PostgreSQL
            var fechaActual = DateTime.Now;
            var fechaInicio = detallesCarrito.First().FechaInicio;
            var fechaFin = detallesCarrito.First().FechaFin;
            
            var subTotal = detallesCarrito.Sum(d => d.SubTotal);
            var total = detallesCarrito.Sum(d => d.Total);
// ... existing code ...