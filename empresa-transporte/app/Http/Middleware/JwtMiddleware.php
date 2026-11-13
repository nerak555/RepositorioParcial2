<?php

namespace App\Http\Middleware;

use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

use Firebase\JWT\JWT;
use Firebase\JWT\Key;

class JwtMiddleware
{
    /**
     * Handle an incoming request.
     *
     * @param  Closure(Request): (Response)  $next
     */
    public function handle(Request $request, Closure $next): Response
    {
        $token = $request->bearerToken();

        if (!$token) {
            return response()->json([
                'mensaje' => 'Token JWT requerido'
            ], 401);
        }
         try {
            $datos = JWT::decode(
                $token,
                new Key(env('JWT_SECRET'), 'HS256')
            );

            $request->attributes->set('usuario_id', $datos->sub);

        } catch (\Throwable $e) {
            return response()->json([
                'mensaje' => 'Token inválido o expirado'
            ], 401);
        }
         return $next($request);
    }
}
