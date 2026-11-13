
package com.mycompany.boleteriajava;

import soap.Central;
import soap.CentralSoap;
import soap.Salida;
import soap.Boleto;
import soap.ArrayOfSalida;

import jakarta.xml.ws.soap.SOAPFaultException;

import javax.swing.*;
import javax.swing.table.DefaultTableModel;
import java.awt.*;
import java.util.ArrayList;
import java.util.List;

public class BoleteriaJava extends JFrame {

    // Cliente SOAP generado automáticamente desde WSDL
    private CentralSoap servicio;

    private JTextField txtDestino;
    private JTextField txtFecha;
    private JTextField txtCI;
    private JTextField txtAsiento;

    private JTable tabla;
    private DefaultTableModel modelo;

    private List<Salida> salidas = new ArrayList<>();

    public BoleteriaJava() {
        setTitle("Boletería - Terminal de Buses de Sucre");
        setSize(850, 500);
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        setLocationRelativeTo(null);

        servicio = new Central().getCentralSoap();

        crearInterfaz();
    }

    private void crearInterfaz() {

        setLayout(new BorderLayout(10, 10));

        // PANEL SUPERIOR
        JPanel superior = new JPanel(new GridLayout(2, 3, 10, 10));

        txtDestino = new JTextField("Cochabamba");
        txtFecha = new JTextField("2026-11-22");

        JButton btnBuscar = new JButton("Buscar salidas");
        btnBuscar.addActionListener(e -> buscarSalidas());

        superior.add(new JLabel("Destino:"));
        superior.add(new JLabel("Fecha (yyyy-MM-dd):"));
        superior.add(new JLabel(""));

        superior.add(txtDestino);
        superior.add(txtFecha);
        superior.add(btnBuscar);

        add(superior, BorderLayout.NORTH);

        // TABLA DE SALIDAS
        modelo = new DefaultTableModel(
            new Object[]{
                "ID", "Destino", "Fecha", "Hora",
                "Bus", "Precio (Bs)", "Libres"
            }, 0
        ) {
            @Override
            public boolean isCellEditable(int fila, int columna) {
                return false;
            }
        };

        tabla = new JTable(modelo);
        tabla.setSelectionMode(
            ListSelectionModel.SINGLE_SELECTION
        );

        add(new JScrollPane(tabla), BorderLayout.CENTER);

        // PANEL DE VENTA
        JPanel inferior = new JPanel(new GridLayout(3, 2, 10, 10));

        txtCI = new JTextField();
        txtAsiento = new JTextField();

        JButton btnVender = new JButton("VENDER BOLETO");
        btnVender.addActionListener(e -> venderBoleto());

        inferior.add(new JLabel("CI del pasajero:"));
        inferior.add(txtCI);

        inferior.add(new JLabel("Número de asiento:"));
        inferior.add(txtAsiento);

        inferior.add(new JLabel("Seleccione una salida en la tabla"));
        inferior.add(btnVender);

        add(inferior, BorderLayout.SOUTH);
    }

    // CONSULTAR CENTRAL SOAP
    private void buscarSalidas() {
        try {
            ArrayOfSalida respuesta = servicio.verSalidas(
                txtDestino.getText().trim(),
                txtFecha.getText().trim()
            );

            modelo.setRowCount(0);
            salidas.clear();

            if (respuesta != null) {
                salidas.addAll(respuesta.getSalida());
            }

            for (Salida s : salidas) {
                modelo.addRow(new Object[]{
                    s.getId(),
                    s.getDestino(),
                    s.getFecha(),
                    s.getHora(),
                    s.getBus(),
                    s.getPrecio(),
                    s.getAsientosLibres()
                });
            }

            if (salidas.isEmpty()) {
                JOptionPane.showMessageDialog(
                    this, "No se encontraron salidas."
                );
            }

        } catch (SOAPFaultException ex) {
            mostrarError(ex.getFault().getFaultString());
        } catch (Exception ex) {
            mostrarError(ex.getMessage());
        }
    }

    // VENDER BOLETO MEDIANTE SOAP
    private void venderBoleto() {

        int fila = tabla.getSelectedRow();

        if (fila == -1) {
            mostrarError("Seleccione una salida de la tabla.");
            return;
        }

        try {
            String ci = txtCI.getText().trim();

            int numero = Integer.parseInt(
                txtAsiento.getText().trim()
            );

            int indice = tabla.convertRowIndexToModel(fila);
            Salida salida = salidas.get(indice);

            // Llamada al SOAP de C#
            Boleto boleto = servicio.venderBoleto(
                ci,
                salida.getId(),
                numero
            );

            // COMPROBANTE
            String comprobante =
                "TERMINAL DE BUSES DE SUCRE\n" +
                "----------------------------------\n" +
                "Código: " + boleto.getCodigo() + "\n" +
                "Pasajero: " + boleto.getNombrePasajero() + "\n" +
                "Destino: " + boleto.getDestino() + "\n" +
                "Fecha: " + boleto.getFecha() + "\n" +
                "Hora: " + boleto.getHora() + "\n" +
                "Asiento: " + boleto.getNumeroAsiento() + "\n" +
                "Precio: Bs " + boleto.getPrecio() + "\n" +
                "----------------------------------\n" +
                "VENTA REALIZADA CORRECTAMENTE";

            JOptionPane.showMessageDialog(
                this,
                comprobante,
                "Comprobante de Venta",
                JOptionPane.INFORMATION_MESSAGE
            );

            // Actualizar asientos libres
            buscarSalidas();

        } catch (NumberFormatException ex) {
            mostrarError("Ingrese un número de asiento válido.");

        } catch (SOAPFaultException ex) {
            mostrarError(ex.getFault().getFaultString());

        } catch (Exception ex) {
            mostrarError(ex.getMessage());
        }
    }

    private void mostrarError(String mensaje) {
        JOptionPane.showMessageDialog(
            this,
            mensaje,
            "Error de Venta",
            JOptionPane.ERROR_MESSAGE
        );
    }

    public static void main(String[] args) {
        SwingUtilities.invokeLater(() -> {
            try {
                new BoleteriaJava().setVisible(true);
            } catch (Exception ex) {
                JOptionPane.showMessageDialog(
                    null,
                    "Error al conectar con Central SOAP:\n"
                        + ex.getMessage()
                );
            }
        });
    }
}

