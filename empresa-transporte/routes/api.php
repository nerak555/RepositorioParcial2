<?php

use Illuminate\Support\Facades\Route;
use App\Http\Controllers\Api\AuthController;
use App\Http\Controllers\Api\SalidaController;
use App\Http\Controllers\Api\AsientoController;

// RUTAS PÚBLICAS
Route::post('/login', [AuthController::class, 'login']);

Route::get('/salidas', [SalidaController::class, 'index']);
Route::get('/salidas/{id}', [SalidaController::class, 'show']);

// RUTAS PROTEGIDAS CON JWT
Route::middleware('jwt')->group(function () {

    Route::post('/salidas', [SalidaController::class, 'store']);

    Route::put('/salidas/{id}/asientos/{numero}', [AsientoController::class, 'reservar']);

});