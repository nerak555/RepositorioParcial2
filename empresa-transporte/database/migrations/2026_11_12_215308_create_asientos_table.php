<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
         Schema::create('asientos', function (Blueprint $table) {
            $table->id();
            $table->foreignId('salida_id')
                ->constrained('salidas')
                ->cascadeOnDelete();
            $table->unsignedInteger('numero');
            $table->string('ci', 10);
            $table->timestamps();

            $table->unique(['salida_id', 'numero']);
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('asientos');
    }
};
