using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheRealBank.Repositories.Users;
using Ent = TheRealBank.Entities.Customer;

namespace TheRealBank.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository repo;

        public CustomerService(ICustomerRepository repo) => this.repo = repo;

        public async Task<List<Customer>> GetCustomersAsync()
            => (await repo.GetAllAsync()).Select(MapToDto).ToList();

        public async Task AddCustomerAsync(Customer newCustomer)
        {
            if (newCustomer == null) throw new ArgumentNullException(nameof(newCustomer));
            await repo.AddAsync(MapToEntity(newCustomer));
        }

        public async Task<Customer> GetCustomerByCpfAsync(string cpf)
        {
            var entity = await repo.GetByCpfAsync(cpf);
            return entity is null ? null! : MapToDto(entity);
        }

        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            var entity = await repo.GetByEmailAsync(email);
            return entity is null ? null : MapToDto(entity);
        }

        public async Task UpdateAsync(string cpf, Customer clienteAtualizado)
        {
            if (clienteAtualizado == null) throw new ArgumentNullException(nameof(clienteAtualizado));
            var entity = await repo.GetByCpfAsync(cpf);
            if (entity is null) return;

            entity.Nome = clienteAtualizado.Nome ?? entity.Nome;
            entity.CPF = clienteAtualizado.CPF ?? entity.CPF;
            entity.Email = clienteAtualizado.Email ?? entity.Email;
            entity.Saldo = clienteAtualizado.Saldo;
            entity.DataNascimento = clienteAtualizado.DataNascimento;
            entity.Senha = clienteAtualizado.Senha ?? entity.Senha;
            entity.Auth = clienteAtualizado.Auth;
            entity.KeyPix = clienteAtualizado.KeyPix ?? entity.KeyPix;

            await repo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(string cpf) => await repo.DeleteByCpfAsync(cpf);

        public async Task PromoteToAdminAsync(string cpf)
        {
            var entity = await repo.GetByCpfAsync(cpf);
            if (entity is null || entity.Auth) return;
            entity.Auth = true;
            await repo.UpdateAsync(entity);
        }

        public async Task DemoteFromAdminAsync(string cpf)
        {
            var entity = await repo.GetByCpfAsync(cpf);
            if (entity is null || !entity.Auth) return;
            entity.Auth = false;
            await repo.UpdateAsync(entity);
        }

        public async Task SetPixKeyAsync(string email, string keyPix)
        {
            var entity = await repo.GetByEmailAsync(email);
            if (entity is null) return;
            entity.KeyPix = keyPix;
            await repo.UpdateAsync(entity);
        }

        public async Task<Customer?> GetCustomerByPixKeyAsync(string keyPix)
        {
            var entity = await repo.GetByPixKeyAsync(keyPix);
            return entity is null ? null : MapToDto(entity);
        }

        public async Task<TransferResult> TransferPixAsync(string senderEmail, string pixKey, decimal amount, string? description = null)
        {
            return await repo.TransferAsync(senderEmail, pixKey, amount, description);
        }

        private static Ent MapToEntity(Customer c) => new Ent
        {
            Nome = c.Nome ?? string.Empty,
            CPF = c.CPF ?? string.Empty,
            Email = c.Email ?? string.Empty,
            Saldo = c.Saldo,
            DataNascimento = c.DataNascimento,
            Senha = c.Senha ?? string.Empty,
            Auth = c.Auth,
            KeyPix = c.KeyPix
        };

        private static Customer MapToDto(Ent e) => new Customer
        {
            Nome = e.Nome,
            CPF = e.CPF,
            Email = e.Email,
            Saldo = e.Saldo,
            DataNascimento = e.DataNascimento,
            Senha = e.Senha,
            Auth = e.Auth,
            KeyPix = e.KeyPix
        };
    }
}