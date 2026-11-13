<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Salida extends Model
{
     protected $fillable = [
        'destino', 'fecha', 'hora',
        'bus', 'precio', 'capacidad'
    ];

    public function asientos()
    {
        return $this->hasMany(Asiento::class);
    }
}
