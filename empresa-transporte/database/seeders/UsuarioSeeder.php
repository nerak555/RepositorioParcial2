<?php

namespace Database\Seeders;

use Illuminate\Database\Console\Seeds\WithoutModelEvents;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;
use App\Models\User;
use App\Models\Salida;

class UsuarioSeeder extends Seeder
{
    public function run(): void
    {
        User::updateOrCreate(
            ['email' => 'admin@empresa.com'],
            [
                'name' => 'Administrador',
                'password' => Hash::make('12345678')
            ]
        );

        $salidas = [
            ['destino' => 'Potosí', 'fecha' => '2026-11-20',
             'hora' => '08:30', 'bus' => 'BUS-101',
             'precio' => 50, 'capacidad' => 40],

            ['destino' => 'La Paz', 'fecha' => '2026-11-20',
             'hora' => '20:00', 'bus' => 'BUS-102',
             'precio' => 120, 'capacidad' => 35],

            ['destino' => 'Santa Cruz', 'fecha' => '2026-11-21',
             'hora' => '18:00', 'bus' => 'BUS-103',
             'precio' => 150, 'capacidad' => 45],
        ];

        foreach ($salidas as $salida) {
            Salida::firstOrCreate(
                [
                    'destino' => $salida['destino'],
                    'fecha' => $salida['fecha'],
                    'hora' => $salida['hora'],
                    'bus' => $salida['bus']
                ],
                $salida
            );
        }
    }
}
