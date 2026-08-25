# Data Ownership

Unity client owns local combat frame simulation only. The server owns permanent data, run creation, wave plans, rewards, validation inputs, and deterministic result validation.

Routes must not write SQL directly and must not calculate costs or rewards directly. Apps call package-level services/adapters; packages must not import apps.
