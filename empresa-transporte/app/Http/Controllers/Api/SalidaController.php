<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;
use App\Models\Salida;

class SalidaController extends Controller
{
    // GET /api/salidas
    public function index(Request $request)
    {
        $consulta = Salida::withCount('asientos');

        if ($request->filled('destino')) {
            $consulta->where(
                'destino', 'LIKE',
                '%' . $request->destino . '%'
            );
        }

        if ($request->filled('fecha')) {
            $consulta->whereDate('fecha', $request->fecha);
        }

        $salidas = $consulta->orderBy('fecha')
            ->orderBy('hora')
            ->get();

        foreach ($salidas as $salida) {
            $salida->asientos_libres =
                $salida->capacidad - $salida->asientos_count;
        }

        return response()->json($salidas);
    }

    // GET /api/salidas/{id}
    public function show($id)
    {
        $salida = Salida::with('asientos')->find($id);

        if (!$salida) {
            return response()->json([
                'mensaje' => 'Salida no encontrada'
            ], 404);
        }

        $salida->asientos_libres =
            $salida->capacidad - $salida->asientos->count();

        return response()->json($salida);
    }

    // POST /api/salidas
    public function store(Request $request)
    {
        $datos = $request->validate([
            'destino' => 'required|string|max:100',
            'fecha' => 'required|date',
            'hora' => 'required|date_format:H:i',
            'bus' => 'required|string|max:100',
            'precio' => 'required|numeric|min:0',
            'capacidad' => 'required|integer|min:1'
        ]);

        $salida = Salida::create($datos);

        return response()->json([
            'mensaje' => 'Salida registrada correctamente',
            'salida' => $salida
        ], 201);
    }
}