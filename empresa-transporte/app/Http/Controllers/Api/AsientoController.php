<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Salida;
use App\Models\Asiento;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Validator;

class AsientoController extends Controller
{
    // PUT /api/salidas/{id}/asientos/{numero}
    public function reservar(Request $request, $id, $numero)
    {
        // Validar CI y número de asiento
        $datos = Validator::make([
            'numero' => $numero,
            'ci' => $request->input('ci')
        ], [
            'numero' => 'required|integer|min:1',
            'ci' => ['required', 'regex:/^\d{5,10}$/']
        ])->validate();

        return DB::transaction(function () use ($id, $numero, $datos) {

            $salida = Salida::whereKey($id)
                ->lockForUpdate()
                ->first();

            if (!$salida) {
                return response()->json([
                    'mensaje' => 'Salida no encontrada'
                ], 404);
            }

            if ((int)$numero > $salida->capacidad) {
                return response()->json([
                    'mensaje' => 'El asiento excede la capacidad del bus'
                ], 422);
            }

            $ocupado = Asiento::where('salida_id', $salida->id)
                ->where('numero', $numero)
                ->exists();

            if ($ocupado) {
                return response()->json([
                    'mensaje' => 'El asiento ya está ocupado'
                ], 409);
            }

            $asiento = Asiento::create([
                'salida_id' => $salida->id,
                'numero' => (int)$numero,
                'ci' => $datos['ci']
            ]);

            return response()->json([
                'mensaje' => 'Asiento reservado correctamente',
                'asiento' => $asiento
            ], 200);
        });
    }
}