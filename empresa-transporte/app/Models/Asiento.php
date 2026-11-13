<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Asiento extends Model
{
    protected $fillable = [
        'salida_id', 'numero', 'ci'
    ];

    public function salida()
    {
        return $this->belongsTo(Salida::class);
    }
}
