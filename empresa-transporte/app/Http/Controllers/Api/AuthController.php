<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\User;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Firebase\JWT\JWT;

class AuthController extends Controller
{
    public function login(Request $request)
    {
        $datos = $request->validate([
            'email' => 'required|email',
            'password' => 'required|string'
        ]);

        $usuario = User::where('email', $datos['email'])->first();

        if (!$usuario || !Hash::check($datos['password'], $usuario->password)) {
            return response()->json([
                'mensaje' => 'Credenciales inválidas'
            ], 401);
        }

        $ahora = time();

        $token = JWT::encode([
            'sub' => $usuario->id,
            'iat' => $ahora,
            'exp' => $ahora + 3600
        ], env('JWT_SECRET'), 'HS256');

        return response()->json([
            'token' => $token,
            'tipo' => 'Bearer',
            'expira_en' => 3600
        ]);
    }
}