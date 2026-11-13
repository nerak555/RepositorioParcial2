const express = require("express");
const cors = require("cors");
const mysql = require("mysql2/promise");
const { buildSchema, graphql, GraphQLError } = require("graphql");

const app = express();
app.use(cors());
app.use(express.json());

const db = mysql.createPool({
  host: "localhost",
  user: "root",
  password: "",
  database: "padron_db"
});

const schema = buildSchema(`
  type Pasajero {
    ci: String!
    nombres: String!
    apellidos: String!
    celular: String!
    frecuente: Boolean!
  }

  type Query {
    pasajero(ci: String!): Pasajero
    pasajeros: [Pasajero!]!
  }

  type Mutation {
    registrarPasajero(
      ci: String!
      nombres: String!
      apellidos: String!
      celular: String!
    ): Pasajero!
  }
`);

const root = {
  pasajero: async ({ ci }) => {
    const [rows] = await db.query(
      "SELECT * FROM pasajeros WHERE ci = ?", [ci]
    );

    if (!rows.length) {
      throw new GraphQLError("Pasajero no registrado");
    }

    return rows[0];
  },

  pasajeros: async () => {
    const [rows] = await db.query("SELECT * FROM pasajeros");
    return rows;
  },

  registrarPasajero: async ({ ci, nombres, apellidos, celular }) => {
    if (!/^\d{5,10}$/.test(ci)) {
      throw new GraphQLError("CI inválido: debe tener entre 5 y 10 dígitos");
    }

    if (!nombres.trim() || !apellidos.trim() || !celular.trim()) {
      throw new GraphQLError("Datos inválidos");
    }

    const [existe] = await db.query(
      "SELECT ci FROM pasajeros WHERE ci = ?", [ci]
    );

    if (existe.length) {
      throw new GraphQLError("El pasajero ya está registrado");
    }

    try {
      await db.query(
        "INSERT INTO pasajeros (ci, nombres, apellidos, celular, frecuente) VALUES (?, ?, ?, ?, 0)",
        [ci, nombres, apellidos, celular]
      );
    } catch (error) {
      if (error.code === "ER_DUP_ENTRY") {
        throw new GraphQLError("El pasajero ya está registrado");
      }
      throw error;
    }

    const [rows] = await db.query(
      "SELECT * FROM pasajeros WHERE ci = ?", [ci]
    );

    return rows[0];
  }
};

app.post("/graphql", async (req, res) => {
  const result = await graphql({
    schema,
    source: req.body.query || "",
    rootValue: root,
    variableValues: req.body.variables
  });

  res.json(result);
});

app.listen(4000, () => {
  console.log("PADRON GRAPHQL: http://localhost:4000/graphql");
});
