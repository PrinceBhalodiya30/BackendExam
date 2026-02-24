create database Ticket_Management

create table roles (
	id int identity(1,1) primary key,
    name varchar(50) not null unique check (name in ('MANAGER', 'SUPPORT', 'USER'))
);

insert into roles (name) values ('manager'), ('support'), ('user');
