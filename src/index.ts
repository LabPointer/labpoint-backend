import { fastify } from "fastify";
import {
  serializerCompiler,
  validatorCompiler,
  jsonSchemaTransform,
  type ZodTypeProvider,
} from "fastify-type-provider-zod";
import { fastifySwagger } from "@fastify/swagger";
import { fastifyCors } from "@fastify/cors";
import ScalarApiReference from "@scalar/fastify-api-reference";
import { routes } from "#api";
import ck from "chalk";

const app = fastify().withTypeProvider<ZodTypeProvider>();

app.setValidatorCompiler(validatorCompiler);
app.setSerializerCompiler(serializerCompiler);

await app.register(fastifyCors, {
  origin: true,
  methods: ["GET", "POST", "DELETE"],
  //credentials: true
});

await app.register(fastifySwagger, {
  openapi: {
    info: {
      title: "Labpoint API",
      description: "Backend do site labpoint",
      version: "0.0.1",
    },
  },
  transform: jsonSchemaTransform,
});

await app.register(ScalarApiReference, {
  routePrefix: "/docs",
});

// Rotas
await routes(app);

await app.ready();

app.listen({ port: 3001, host: "0.0.0.0" }).then(() => {
  console.log(
    ck.greenBright("🔥 HTTP server running on http://localhost:3001"),
  );
  console.log(ck.yellow("📚 Docs avaliable at http://localhost:3001/docs"));
});
