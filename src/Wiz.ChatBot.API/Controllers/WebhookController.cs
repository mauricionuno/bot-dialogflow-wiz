using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wiz.ChatBot.API.Services;
using Wiz.ChatBot.API.Services.Interfaces;
using Wiz.ChatBot.API.ViewModels;

namespace Wiz.ChatBot.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IFuncionarioService _funcionarioService;
        public WebhookController(IFuncionarioService funcionarioService)
        {
            _funcionarioService = funcionarioService;
        }

        // A Protobuf JSON parser configured to ignore unknown fields. This makes
        // the action robust against new fields being introduced by Dialogflow.
        private static readonly JsonParser jsonParser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
        [HttpPost]
        public async Task<ContentResult> DialogAction()
        {
            // Parse the body of the request using the Protobuf JSON parser,
            // *not* Json.NET.
            WebhookRequest request;
            using (var reader = new StreamReader(Request.Body))
            {
                request = jsonParser.Parse<WebhookRequest>(reader);
            }
            string retorno = "";

            var requestParameters = request.QueryResult.Parameters;

            if ((request.QueryResult.Intent.DisplayName == "autenticar"))
            {
                var cpf = requestParameters.Fields["cpf"].StringValue;
                var funcionarioAuth = await _funcionarioService.GetFuncionario(cpf);
                if (funcionarioAuth != null)
                {
                    if (funcionarioAuth.sexo == "M")
                    {
                        retorno = $"Seja bem vindo {funcionarioAuth.nome.Split(" ")[0]}!";
                    }
                    else
                    {
                        retorno = $"Seja bem vinda {funcionarioAuth.nome.Split(" ")[0]}!";
                    }

                    retorno += @" Aqui é um lugar para você tirar as suas duvidas. Como posso te ajudar?";
                }
                else
                {
                    retorno = "Ops! Não encontrei o seu cpf, tem certeza que digitou certo? por favor digite novamente!";

                }

                WebhookResponse res = new WebhookResponse
                {
                    FulfillmentText = retorno
                };

                string resJson = res.ToString();
                return Content(resJson, "application/json");
            }

            FuncionarioViewModel funcionario = null;
            var requestOutputContext = request.QueryResult.OutputContexts.Where(c => c.Name.Contains("autenticado")).FirstOrDefault();
            if (requestOutputContext != null)
            {
                var cpf = requestOutputContext.Parameters.Fields["cpf"].StringValue;
                funcionario = await _funcionarioService.GetFuncionario(cpf);
                if (funcionario == null)
                {
                    WebhookResponse res = new WebhookResponse
                    {
                        FulfillmentText = "Ainda não sei quem é você, me passa seu CPF!"
                    };
                    res.OutputContexts.Add(new Context
                    {
                        ContextName = requestOutputContext.ContextName,
                        LifespanCount = 0

                    }
                );
                    string resJson = res.ToString();
                    return Content(resJson, "application/json");
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "email"))
            {
                var cpf = requestOutputContext.Parameters.Fields["cpf"].StringValue;
                var email = (await _funcionarioService.GetFuncionario(cpf)).email;
                retorno = $"O seu email é: {email}";
            }
            if ((request.QueryResult.Intent.DisplayName == "gestor-quem"))
            {
                var cpf = requestOutputContext.Parameters.Fields["cpf"].StringValue;
                var gestor = (await _funcionarioService.GetFuncionario(cpf)).gestor;
                var padrinho = (await _funcionarioService.GetFuncionario(cpf)).padrinho;
                retorno = $"{gestor}! Mas lembre-se que você pode sempre contar com o seu padrinho {padrinho}";
            }
            if ((request.QueryResult.Intent.DisplayName == "matricula"))
            {
                var cpf = requestOutputContext.Parameters.Fields["cpf"].StringValue;
                var matricula = (await _funcionarioService.GetFuncionario(cpf)).matricula;
                retorno = $"Sua matricula da Wiz é: {matricula}";
            }
            if ((request.QueryResult.Intent.DisplayName == "primeirodia-quando"))
            {
                var cpf = requestOutputContext.Parameters.Fields["cpf"].StringValue;

                var stringDataInicio = (await _funcionarioService.GetFuncionario(cpf)).datainicio;
                var dataInicio = DateTime.Parse(stringDataInicio);
                TimeSpan timeSpan = dataInicio - (DateTime.ParseExact(DateTime.Now.ToShortDateString(), "dd/MM/yyyy", CultureInfo.InvariantCulture));
                if (timeSpan.Days < -1)
                {
                    retorno = $"Poxa! Acho que você esta meio atrasado. Foi dia {dataInicio.Day}/{dataInicio.Month}/{dataInicio.Year}.";
                }
                if (timeSpan.Days == -1)
                {
                    retorno = $"Poxa! Acho que você esta meio atrasado. Foi ontem.";
                }
                if (timeSpan.Days == 0)
                {
                    retorno = $"Uhul. É Hoje!";
                }
                if (timeSpan.Days == 1)
                {
                    retorno = $"Uhul. É amanhã!";
                }
                if (timeSpan.Days == 2)
                {
                    retorno = $"Uhul. É daqui 2 dias!";
                }
                if (timeSpan.Days > 2)
                {
                    retorno = $"Seu primeiro dia de trabalho é {dataInicio.Day}/{dataInicio.Month}/{dataInicio.Year}";
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "beneficios"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/matriz que você consegue encontrar tudo que precisa saber sobre os benefícios oferecidos pela Wiz Corporativo.";
                        break;
                    case "bpo":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/bpo que você consegue encontrar tudo que precisa saber sobre os benefícios oferecidos pela Wiz BPO.";
                        break;
                    case "b2u":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/b2u que você consegue encontrar tudo que precisa saber sobre os benefícios oferecidos pela Wiz B2U.";
                        break;
                    case "corporate":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/corporate que você consegue encontrar tudo que precisa saber sobre os benefícios oferecidos pela Wiz Corporate.";
                        break;
                    case "parceiros":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros que você consegue encontrar tudo que precisa saber sobre os benefícios oferecidos pela Wiz Parceiros.";
                        break;
                    case "rede":
                        retorno = @"Os benefícios oferecidos pela Rede são: Vale Transporte,
                                    Vale Refeição ou Alimentação(R$504, 73),
                                    Cesta Básica(R$299,09),
                                    Plano de Saúde,
                                    Plano Odontológico,
                                    Convênio Farmácia,
                                    Seguro de Vida,
                                    Auxílio Creche(R$410,28 para cada filho até 7 anos, ou seja, 6 anos incompletos) e
                                    Participação nos lucros.";
                        break;

                    default:
                        break;
                }
            }


            if ((request.QueryResult.Intent.DisplayName == "vt-comorecebo"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = @"No seu primeiro mês como wizzer, o valor será creditado na sua conta no prazo de 7 dias úteis a partir da admissão. Não esquece de conferir, tá? (;
                                    Nos meses subsequentes o valor será creditado no cartão transporte.";
                        break;
                    case "bpo":
                        retorno = @"No seu primeiro mês como wizzer, o valor será creditado na sua conta no prazo de 7 dias úteis a partir da admissão. Não esquece de conferir, tá? (;
                                    Nos meses subsequentes o valor será creditado no cartão transporte.";
                        break;
                    case "b2u":
                        retorno = @"No seu primeiro mês como wizzer, o valor será creditado na sua conta no prazo de 7 dias úteis a partir da admissão. Não esquece de conferir, tá? (;
                                    Nos meses subsequentes o valor será creditado no cartão transporte.";
                        break;
                    case "corporate":
                        retorno = @"No seu primeiro mês como wizzer, o valor será creditado na sua conta no prazo de 7 dias úteis a partir da admissão. Não esquece de conferir, tá? (;
                                    Nos meses subsequentes o valor será creditado no cartão transporte.";
                        break;
                    case "parceiros":
                        retorno = @"No seu primeiro mês como wizzer, o valor será creditado na sua conta no prazo de 7 dias úteis a partir da admissão. Não esquece de conferir, tá? (;
                                    Nos meses subsequentes o valor será creditado no cartão transporte.";
                        break;
                    case "rede":
                        retorno = @"No seu primeiro mês como wizzer, o valor será creditado na sua conta até o final do mês de admissão. Não esquece de conferir, tá? (;
                                    Nos meses subsequentes o valor será creditado no cartão transporte informado por você, como Bilhete Único, BOM, entre outros.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "vr-recebo"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = @"Aqui na Wiz você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.

                                    No seu primeiro mês como Wizzer, o valor proporcional aos dias trabalhados no mês será creditado no cartão VR.

                                    Você receberá o(s) cartão(ões) escolhido(s) no prazo de até 10 dias úteis após sua admissão. Sabe onde pegar? É só ir ao COP, que fica no préfio do Liberty Mall 13 Andar ;)

                                    Ah, só lembrando que este benefício pode ser recebido de 03 formas:

                                    01. Crédito de 100% no cartão VR Alimentação; ou

                                    02. Crédito de 100% no cartão VR Refeição; ou

                                    03. Divisão do crédito em 50% no VR Alimentação e 50% no VR Refeição";
                        break;
                    case "bpo":
                        retorno = @"Aqui na Wiz BPO você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.

                                    No seu primeiro mês como Wizzer, o valor proporcional aos dias trabalhados no mês será creditado no cartão VR.

                                    Você receberá o(s) cartão(ões) escolhido(s) no prazo de até 10 dias úteis após sua admissão.

                                    Ah, só lembrando que este benefício pode ser recebido de 03 formas:

                                    01. Crédito de 100% no cartão VR Alimentação; ou

                                    02. Crédito de 100% no cartão VR Refeição; ou

                                    03. Divisão do crédito em 50% no VR Alimentação e 50% no VR Refeição

                                    Se você tiver optado por dividir o valor do benefício, você receberá dois cartões, VR Alimentação e VR Refeição. Caso contrário o valor total será creditado no cartão escolhido.

                                    Pra pegar os cartões é só esperar por um e-mail do Centro de Operação de Pessoas – COP informando que o(s) cartão(ões) está(ão) disponível(veis) para retirada. Caso você não fique em Brasília o COP irá enviar por meio de carta registrada o(s) cartão(ões).";
                        break;
                    case "b2u":
                        retorno = @"Aqui na Wiz B2U você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.

                                    No seu primeiro mês como Wizzer, o valor proporcional aos dias trabalhados no mês será creditado no cartão VR.

                                    Você receberá o(s) cartão(ões) escolhido(s) no prazo de até 10 dias úteis após sua admissão. Sabe onde pegar? É só ir ao COP, que fica no préfio do Liberty Mall 13 Andar ;)

                                    Ah, só lembrando que este benefício pode ser recebido de 03 formas:

                                    01. Crédito de 100% no cartão VR Alimentação; ou

                                    02. Crédito de 100% no cartão VR Refeição; ou

                                    03. Divisão do crédito em 50% no VR Alimentação e 50% no VR Refeição

                                    Se você tiver optado por dividir o valor do benefício, você receberá dois cartões, VR Alimentação e VR Refeição. Caso contrário o valor total será creditado no cartão escolhido.";
                        break;
                    case "corporate":
                        retorno = @"Aqui na Wiz Corporate você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.

                                    No seu primeiro mês como Wizzer, o valor proporcional aos dias trabalhados no mês será creditado no cartão VR.

                                    Você receberá o(s) cartão(ões) escolhido(s) no prazo de até 10 dias úteis após sua admissão. Sabe onde pegar?

                                    Ah, só lembrando que este benefício pode ser recebido de 03 formas:

                                    01. Crédito de 100% no cartão VR Alimentação; ou

                                    02. Crédito de 100% no cartão VR Refeição; ou

                                    03. Divisão do crédito em 50% no VR Alimentação e 50% no VR Refeição, sendo assim receberá R$366,30 em cada cartão.";
                        break;
                    case "parceiros":
                        retorno = @"Aqui na Wiz Parceiros você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.

                                    • No seu primeiro mês como Wizzer, o valor proporcional aos dias trabalhados no mês será creditado no cartão VR.

                                    • Você receberá o(s) cartão(ões) escolhido(s) no prazo de até 10 dias úteis após sua admissão. Sabe onde pegar? É só ir ao COP, que fica no préfio do Liberty Mall 13 Andar ;)

                                    • Ah, só lembrando que este benefício pode ser recebido de 03 formas:

                                    01. Crédito de 100% no cartão VR Alimentação; ou

                                    02. Crédito de 100% no cartão VR Refeição; ou

                                    03. Divisão do crédito em 50% no VR Alimentação e 50% no VR Refeição

                                    Se você tiver optado por dividir o valor do benefício, você receberá dois cartões, VR Alimentação e VR Refeição. Caso contrário o valor total será creditado no cartão escolhido.";
                        break;
                    case "rede":
                        retorno = @"O seu cartão chegará na sua Unidade Regional e será entregue em mãos ou será enviado para a sua agência de trabalho via malote.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "vr-vrouva"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = @"Aqui na Wiz você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.";
                        break;
                    case "bpo":
                        retorno = @"Aqui na Wiz BPO você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.";
                        break;
                    case "b2u":
                        retorno = @"Aqui na Wiz B2U você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.";
                        break;
                    case "corporate":
                        retorno = @"Aqui na Wiz Corporate você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.";
                        break;
                    case "parceiros":
                        retorno = @"Aqui na Wiz Parceiros você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.";
                        break;
                    case "rede":
                        retorno = @"Aqui na Rede você pode escolher como receber o valor do vale refeição de acordo com o que você precisa, podendo este valor ser dividido no modelo de Vale Refeição e Vale Alimentação. O Vale refeição costuma ser aceito em restaurantes e padarias enquanto o vale Alimentação é aceitos em mercados.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "vr-quanto"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = @"O valor mensal do vale refeição é de R$ 732,27 depositados apenas no cartão VR. Se você tiver optado por dividir o valor, receberá R$366,13 no cartão VR Alimentação e R$366,13 no cartão VR Refeição.";
                        break;
                    case "bpo":
                        retorno = "O valor mensal do vale refeição é de R$ 732,60 depositados apenas no cartão VR. Se você tiver optado por dividir o valor, receberá R$366,30 no cartão VR Alimentação e R$366,13 no cartão VR Refeição.";
                        break;
                    case "b2u":
                        retorno = " O valor diário é de R$33,00 e é creditado proporcionalmente a quantidade de dias trabalhados no mês.";
                        break;
                    case "corporate":
                        retorno = "O valor mensal do vale refeição é de R$ 732,60 depositados apenas no cartão VR. Se você tiver optado por dividir o valor, receberá R$366,30 no cartão VR Alimentação e R$366,13 no cartão VR Refeição.";
                        break;
                    case "parceiros":
                        retorno = @"O valor mensal do vale refeição é de R$ 732,27 depositados apenas no cartão VR. Se você tiver optado por dividir o valor, receberá R$366,13 no cartão VR Alimentação e R$366,13 no cartão VR Refeição.";
                        break;
                    case "rede":
                        retorno = "O valor mensal é de R$ 504,73.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "vr-quando"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "Todo dia 30 de cada mês sem falta!";
                        break;
                    case "bpo":
                        retorno = "Todo dia 30 de cada mês sem falta!";
                        break;
                    case "b2u":
                        retorno = "Todo dia 30 de cada mês sem falta!";
                        break;
                    case "corporate":
                        retorno = "Todo dia 30 de cada mês sem falta!";
                        break;
                    case "parceiros":
                        retorno = "Todo dia 30 de cada mês sem falta!";
                        break;
                    case "rede":
                        retorno = @"No seu primeiro mês como wizzer, o valor será creditado aproximadamente oito dias após a sua admissão.
                                    Nos meses subsequentes o valor será creditado todo final de mês.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "saude-qual"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/matriz que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "bpo":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/bpo que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "b2u":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/b2u que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "corporate":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/corporate que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "parceiros":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "rede":
                        retorno = @"O plano de saúde pode ser  Amil ou Unimed. Só lembrando que O prazo para inclusão sem carência de dependentes é de 20 dias após a admissão/casamento/nascimento e deve constar obrigatoriamente CPF e CNS do dependente. Se não solicitado na admissão, a solicitação deve ser feita junto ao COP.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "saude-coparticipacao"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = @"É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/matriz que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "bpo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/bpo que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "b2u":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/b2u que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "corporate":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/corporate que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "parceiros":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "rede":
                        retorno = "A coparticipação é com desconto de 20 % quando houver uso(exames simples e consultas, como atendimento ambulatorial, de urgência e emergência).";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "saude-procedimentos"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/matriz que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "bpo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/bpo que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "b2u":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/b2u que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "corporate":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/corporate que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "parceiros":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "rede":
                        retorno = "Exames simples e consultas, como atendimento ambulatorial, de urgência e emergência(de acordo com o prestador).";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "saude-reembolso"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/matriz que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "bpo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/bpo que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "b2u":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/b2u que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "corporate":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/corporate que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "parceiros":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "rede":
                        retorno = "Exames simples e consultas, como atendimento ambulatorial, de urgência e emergência(de acordo com o prestador).";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "saude-app"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/matriz que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "bpo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/bpo que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "b2u":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/b2u que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "corporate":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/corporate que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "parceiros":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "rede":
                        retorno = "Exames simples e consultas, como atendimento ambulatorial, de urgência e emergência(de acordo com o prestador).";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "saude-dep-quem"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/matriz que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "bpo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/bpo que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "b2u":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/b2u que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "corporate":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/corporate que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "parceiros":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "rede":
                        retorno = "Exames simples e consultas, como atendimento ambulatorial, de urgência e emergência(de acordo com o prestador).";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "saude-dep-como"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/matriz que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "bpo":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/bpo que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "b2u":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/b2u que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "corporate":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/corporate que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "parceiros":
                        retorno = "É só clicar nesse link Você pode checar as informações sobre o seu plano de saúde nesse link: https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros que você consegue encontrar tudo que precisa saber sobre o seu plano de saúde.";
                        break;
                    case "rede":
                        retorno = "Exames simples e consultas, como atendimento ambulatorial, de urgência e emergência(de acordo com o prestador).";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "odonto-qual"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "Rede Odonto Empresas";
                        break;
                    case "bpo":
                        retorno = "Rede Odonto Empresas";
                        break;
                    case "b2u":
                        retorno = "Rede Odonto Empresas";
                        break;
                    case "corporate":
                        retorno = "Rede Odonto Empresas";
                        break;
                    case "parceiros":
                        retorno = "Rede Odonto Empresas";
                        break;
                    case "rede":
                        retorno = "Caixa Seguradora Odonto Empresas";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "creche-valor"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "O nosso auxílio creche tem valor mensal de R$ 410,27 e será creditado em conta junto com o salário.";
                        break;
                    case "bpo":
                        retorno = "O nosso auxílio creche tem valor mensal de R$ 250,00 e será creditado em conta junto com o salário.";
                        break;
                    case "b2u":
                        retorno = "O nosso auxílio creche tem valor mensal de R$ 250,00 e será creditado em conta junto com o salário.";
                        break;
                    case "corporate":
                        retorno = "O nosso auxílio creche tem valor mensal de R$ 410,27 e será creditado em conta junto com o salário.";
                        break;
                    case "parceiros":
                        retorno = "O nosso auxílio creche tem valor mensal de R$ 410,27 e será creditado em conta junto com o salário.";
                        break;
                    case "rede":
                        retorno = "O nosso auxílio creche tem valor mensal de R$ 410,28 e será creditado em conta junto com o salário.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "creche-validade"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "O benefício é válido para filhos que tenham idades de 3 (três) meses completos a 7 (sete) anos incompletos, além de estarem matriculados em creches/instituições de livre escolha, independentemente de comprovação de matrícula.";
                        break;
                    case "bpo":
                        retorno = "O benefício é válido para filhos que tenham idades de 4 (três) meses completos a 6 (seis) anos incompletos, além de estarem matriculados em creches/instituições de livre escolha.";
                        break;
                    case "b2u":
                        retorno = "O benefício é válido para filhos que tenham idades de 4 (três) meses completos a 6 (seis) anos incompletos, além de estarem matriculados em creches/instituições de livre escolha.";
                        break;
                    case "corporate":
                        retorno = "O benefício é válido para filhos que tenham idades de 4 (três) meses completos a 6 (seis) anos incompletos, além de estarem matriculados em creches/instituições de livre escolha.";
                        break;
                    case "parceiros":
                        retorno = "O benefício é válido para filhos que tenham idades de 4 (três) meses completos a 6 (seis) anos incompletos, além de estarem matriculados em creches/instituições de livre escolha.";
                        break;
                    case "rede":
                        retorno = "O benefício é válido para filhos que tenham idades de 3 (três) meses completos a 6 (seis) anos incompletos, além de estarem matriculados em creches/instituições de livre escolha.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "seguro-vida-custo"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "Não há qualquer desconto na folha de pagamento para participação.";
                        break;
                    case "bpo":
                        retorno = "Não há qualquer desconto na folha de pagamento para participação.";
                        break;
                    case "b2u":
                        retorno = "Não há qualquer desconto para participação.";
                        break;
                    case "corporate":
                        retorno = "Não há qualquer desconto para participação.";
                        break;
                    case "parceiros":
                        retorno = "Não há qualquer desconto para participação.";
                        break;
                    case "rede":
                        retorno = "Não há qualquer desconto para participação.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "seguro-vida-beneficiarios"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/matriz que você consegue encontrar tudo que precisa saber sobre o seu seguro de vida.";
                        break;
                    case "bpo":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/bpo que você consegue encontrar tudo que precisa saber sobre o seu seguro de vida.";
                        break;
                    case "b2u":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/b2u que você consegue encontrar tudo que precisa saber sobre o seu seguro de vida.";
                        break;
                    case "corporate":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/corporate que você consegue encontrar tudo que precisa saber sobre o seu seguro de vida.";
                        break;
                    case "parceiros":
                        retorno = "É só clicar nesse link https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros que você consegue encontrar tudo que precisa saber sobre o seu seguro de vida.";
                        break;
                    case "rede":
                        retorno = "O colaborador é quem escolhe os benefíários.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "funeral-valor"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = @"A assistência funeral não tem custo para você e o valor que você recebe em caso de sinistro só pode ser informado pela seguradora. 
                                    Entre em contato pelo Serviço de Assistência Funeral (SAF) da Caixa Seguradora.
                                    Informações sobre assistência funeral: https://facil.wizsolucoes.com.br/modeloBeneficio/matriz";
                        break;
                    case "bpo":
                        retorno = @"A assistência funeral não tem custo para você e o valor que você recebe em caso de sinistro só pode ser informado pela seguradora.
                                    Entre em contato pelo Serviço de Assistência Funeral (SAF) da Caixa Seguradora.
                                    Informações sobre assistência funeral: https://facil.wizsolucoes.com.br/modeloBeneficio/bpo";
                        break;
                    case "b2u":
                        retorno = @"A assistência funeral não tem custo para você e o valor que você recebe em caso de sinistro só pode ser informado pela seguradora. 
                                    Entre em contato pelo Serviço de Assistência Funeral (SAF) da Caixa Seguradora.
                                    Informações sobre assistência funeral: https://facil.wizsolucoes.com.br/modeloBeneficio/b2u";
                        break;
                    case "corporate":
                        retorno = @"A assistência funeral não tem custo para você e o valor que você recebe em caso de sinistro só pode ser informado pela seguradora. 
                                    Entre em contato pelo Serviço de Assistência Funeral (SAF) da Caixa Seguradora.
                                    Informações sobre assistência funeral: https://facil.wizsolucoes.com.br/modeloBeneficio/corporate";
                        break;
                    case "parceiros":
                        retorno = @"A assistência funeral não tem custo para você e o valor que você recebe em caso de sinistro só pode ser informado pela seguradora. 
                                    Entre em contato pelo Serviço de Assistência Funeral (SAF) da Caixa Seguradora.
                                    Informações sobre assistência funeral: https://facil.wizsolucoes.com.br/modeloBeneficio/parceiros";
                        break;
                    case "rede":
                        retorno = @"A assistência funeral não tem custo para você e o valor que você recebe em caso de sinistro só pode ser informado pela seguradora. 
                                    Entre em contato pelo Serviço de Assistência Funeral (SAF) da Caixa Seguradora.
                                    Informações sobre assistência funeral: https://facil.wizsolucoes.com.br/modeloBeneficio/rede";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "previdencia-contribuicao"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "Valor mínimo: R$ 35,00.";
                        break;
                    case "bpo":
                        retorno = "Valor mínimo: R$ 35,00.";
                        break;
                    case "b2u":
                        retorno = "Valor mínimo: R$ 35,00.";
                        break;
                    case "corporate":
                        retorno = "Valor mínimo: R$ 35,00.";
                        break;
                    case "parceiros":
                        retorno = "Valor mínimo: R$ 35,00.";
                        break;
                    case "rede":
                        retorno = "Valor mínimo: R$ 35,00.";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "previdencia-quem"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "Todos os colaboradores";
                        break;
                    case "bpo":
                        retorno = "Todos os colaboradores";
                        break;
                    case "b2u":
                        retorno = "Todos os colaboradores";
                        break;
                    case "corporate":
                        retorno = "Todos os colaboradores";
                        break;
                    case "parceiros":
                        retorno = "Todos os colaboradores";
                        break;
                    case "rede":
                        retorno = "Todos os colaboradores";
                        break;

                    default:
                        break;
                }
            }

            if ((request.QueryResult.Intent.DisplayName == "previdencia-pagamento"))
            {
                switch (funcionario.unidade.ToLower())
                {
                    case "corporativo":
                        retorno = "O custeio é realizado integralmente pelo colaborador, com desconto em folha de pagamento.";
                        break;
                    case "bpo":
                        retorno = "";
                        break;
                    case "b2u":
                        retorno = "O custeio é realizado integralmente pelo colaborador, com desconto em folha de pagamento no final do mês (dia 30) e repassado para a seguradora no dia 25 do mês seguinte.";
                        break;
                    case "corporate":
                        retorno = "O custeio é realizado integralmente pelo colaborador, com desconto em folha de pagamento no final do mês (dia 30) e repassado para a seguradora no dia 25 do mês seguinte.";
                        break;
                    case "parceiros":
                        retorno = "O custeio é realizado integralmente pelo colaborador, com desconto em folha de pagamento no final do mês (dia 30) e repassado para a seguradora no dia 25 do mês seguinte.";
                        break;
                    case "rede":
                        retorno = "O custeio é realizado pelo colaborador, com desconto em folha de pagamento e o repasse  será feito para a Seguradora em até 40 dias após o primeiro desconto.";
                        break;

                    default:
                        break;
                }
            }

            // Populate the response
            WebhookResponse response = new WebhookResponse
            {
                FulfillmentText = retorno
            };
            // Ask Protobuf to format the JSON to return.
            // Again, we don’t want to use Json.NET — it doesn’t know how to handle Struct
            // values etc.
            string responseJson = response.ToString();
            return Content(responseJson, "application/json");
        }
    }
}
